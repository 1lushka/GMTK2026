using ForgettingBoxer.Knockout;
using UnityEngine;

[RequireComponent(typeof(TopDownController), typeof(ImpulseReceiver))]
public sealed class PlayerImpulseFlight : MonoBehaviour
{
    [SerializeField] private Transform flightVisual;
    [SerializeField, Min(0.01f)] private float standUpDuration = 0.08f;

    private TopDownController controller;
    private ImpulseReceiver receiver;
    private ActiveAbility[] abilities;
    private bool[] abilityStates;
    private Collider[] attackTriggers;
    private bool[] triggerStates;
    private Quaternion standingRotation;
    private bool controlsLocked;

    private void Awake()
    {
        controller = GetComponent<TopDownController>();
        receiver = GetComponent<ImpulseReceiver>();
        abilities = GetComponents<ActiveAbility>();
        abilityStates = new bool[abilities.Length];
        CacheAttackTriggers();

        if (flightVisual == null && controller.Animator != null)
            flightVisual = controller.Animator.transform;
        if (flightVisual != null)
            standingRotation = flightVisual.localRotation;
    }

    private void OnEnable()
    {
        receiver.ImpulseApplied += OnImpulseApplied;
    }

    private void OnDisable()
    {
        receiver.ImpulseApplied -= OnImpulseApplied;
    }

    private void OnImpulseApplied(ImpulseInfo impulse)
    {
        KnockoutAPI.AddStar();
    }

    private void Update()
    {
        bool shouldLock = receiver.IsMoving;
        if (shouldLock != controlsLocked)
            SetControlsLocked(shouldLock);
    }

    private void LateUpdate()
    {
        if (flightVisual == null) return;

        if (receiver.CurrentState == ImpulseReceiver.State.Flying)
        {
            Vector2 velocity = receiver.CurrentVelocity;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                Vector3 direction = new Vector3(velocity.x, 0f, velocity.y).normalized;
                flightVisual.rotation = Quaternion.FromToRotation(flightVisual.up, direction) *
                    flightVisual.rotation;
            }
            return;
        }

        flightVisual.localRotation = Quaternion.RotateTowards(flightVisual.localRotation,
            standingRotation, 180f / standUpDuration * Time.deltaTime);
    }

    private void SetControlsLocked(bool locked)
    {
        controlsLocked = locked;
        controller.enableMovement = !locked;
        controller.ActivateRotation(!locked);
        controller.SetVelocity(Vector3.zero);

        SetAbilitiesEnabled(!locked);
        SetAttackTriggersEnabled(!locked);

        if (!locked && flightVisual != null)
            flightVisual.localRotation = standingRotation;
    }

    private void SetAbilitiesEnabled(bool enabledState)
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            if (!enabledState)
            {
                abilityStates[i] = abilities[i].enabled;
                abilities[i].enabled = false;
            }
            else
            {
                abilities[i].enabled = abilityStates[i];
            }
        }
    }

    private void CacheAttackTriggers()
    {
        Collider[] childColliders = GetComponentsInChildren<Collider>(true);
        int count = 0;
        for (int i = 0; i < childColliders.Length; i++)
        {
            if (childColliders[i].transform != transform && childColliders[i].isTrigger)
                count++;
        }

        attackTriggers = new Collider[count];
        triggerStates = new bool[count];
        int index = 0;
        for (int i = 0; i < childColliders.Length; i++)
        {
            if (childColliders[i].transform == transform || !childColliders[i].isTrigger) continue;
            attackTriggers[index++] = childColliders[i];
        }
    }

    private void SetAttackTriggersEnabled(bool enabledState)
    {
        for (int i = 0; i < attackTriggers.Length; i++)
        {
            if (attackTriggers[i] == null) continue;

            if (!enabledState)
            {
                triggerStates[i] = attackTriggers[i].enabled;
                attackTriggers[i].enabled = false;
            }
            else
            {
                attackTriggers[i].enabled = triggerStates[i];
            }
        }
    }
}
