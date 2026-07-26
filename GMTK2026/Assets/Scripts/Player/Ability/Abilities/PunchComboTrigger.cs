using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PunchComboTrigger : MonoBehaviour
{
    [Header("Damage")]
    [Min(0f)]
    [SerializeField] private float damagePerSecond = 10f;

    [Header("Lift")]
    [Min(0f)]
    [SerializeField] private float liftHeight = 1.5f;
    [Min(0f)]
    [SerializeField] private float liftDuration = 0.15f;
    [Min(0f)]
    [SerializeField] private float returnDuration = 0.2f;
    [Min(0f)]
    [SerializeField] private float hoverAmplitude = 0.15f;
    [Min(0.01f)]
    [SerializeField] private float hoverPeriod = 0.6f;
    [SerializeField] private Ease liftEase = Ease.OutQuad;
    [SerializeField] private Ease returnEase = Ease.InQuad;

    [Header("Final Hit")]
    [Tooltip("Impulse force. The target profile converts it to speed.")]
    [Min(0f)]
    [SerializeField] private float impulseDistance = 4f;

    private sealed class TargetState
    {
        public readonly HashSet<Collider> Colliders = new HashSet<Collider>();
        public HealthComponent Health;
        public PunchComboTargetMotion Motion;
        public ImpulseReceiver ImpulseReceiver;
        public bool HasExternalControl;
        public float AccumulatedDamage;
    }

    private readonly Dictionary<HealthComponent, TargetState> targets =
        new Dictionary<HealthComponent, TargetState>();
    private bool attackActive;

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        if (!trigger.isTrigger)
        {
            Debug.LogWarning("PunchComboTrigger collider was not a trigger. It has been changed automatically.", this);
            trigger.isTrigger = true;
        }
    }

    public void BeginAttack()
    {
        CleanupTargets(false);
        attackActive = true;
    }

    public void FinalHit(Vector3 direction)
    {
        if (!attackActive) return;

        attackActive = false;
        foreach (TargetState target in targets.Values)
        {
            if (target.Health == null) continue;

            target.Health.SetStunned(false);
            if (target.ImpulseReceiver != null)
            {
                target.Motion.ReleaseForImpulse();
                if (target.HasExternalControl)
                    target.ImpulseReceiver.EndExternalControl();
                Vector2 impulseDirection = new Vector2(direction.x, direction.z);
                target.ImpulseReceiver.ApplyImpulse(impulseDirection, impulseDistance, transform.root.gameObject);
            }
            else
            {
                target.Motion.ReturnToGround(returnDuration, returnEase);
            }
        }

        targets.Clear();
    }

    private void Update()
    {
        if (!attackActive || targets.Count == 0) return;

        List<HealthComponent> invalidTargets = null;
        foreach (KeyValuePair<HealthComponent, TargetState> pair in targets)
        {
            TargetState target = pair.Value;
            if (target.Health == null)
            {
                invalidTargets ??= new List<HealthComponent>();
                invalidTargets.Add(pair.Key);
                continue;
            }

            target.AccumulatedDamage += damagePerSecond * Time.deltaTime;
            int wholeDamage = Mathf.FloorToInt(target.AccumulatedDamage);
            if (wholeDamage <= 0) continue;

            target.AccumulatedDamage -= wholeDamage;
            target.Health.TakeDamage(wholeDamage);
        }

        if (invalidTargets == null) return;
        foreach (HealthComponent target in invalidTargets)
            targets.Remove(target);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!attackActive) return;

        HealthComponent health = other.GetComponentInParent<HealthComponent>();
        if (health == null || health.transform.IsChildOf(transform.root)) return;

        if (!targets.TryGetValue(health, out TargetState target))
        {
            PunchComboTargetMotion motion = health.GetComponent<PunchComboTargetMotion>();
            if (motion == null)
                motion = health.gameObject.AddComponent<PunchComboTargetMotion>();

            target = new TargetState
            {
                Health = health,
                Motion = motion,
                ImpulseReceiver = health.GetComponent<ImpulseReceiver>()
            };
            if (target.ImpulseReceiver != null)
                target.HasExternalControl = target.ImpulseReceiver.BeginExternalControl();
            targets.Add(health, target);
            health.SetStunned(true);
            motion.Lift(liftHeight, liftDuration, liftEase, hoverAmplitude, hoverPeriod);
        }

        target.Colliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        HealthComponent health = other.GetComponentInParent<HealthComponent>();
        if (health == null || !targets.TryGetValue(health, out TargetState target)) return;

        target.Colliders.Remove(other);
        if (target.Colliders.Count > 0) return;

        ReleaseTarget(target);
        targets.Remove(health);
    }

    private void ReleaseTarget(TargetState target)
    {
        if (target.Health != null)
            target.Health.SetStunned(false);
        if (target.HasExternalControl && target.ImpulseReceiver != null)
            target.ImpulseReceiver.EndExternalControl();
        if (target.Motion != null)
            target.Motion.ReturnToGround(returnDuration, returnEase);
    }

    private void CleanupTargets(bool keepAttackActive)
    {
        foreach (TargetState target in targets.Values)
            ReleaseTarget(target);

        targets.Clear();
        attackActive = keepAttackActive;
    }

    private void OnDisable()
    {
        CleanupTargets(false);
    }
}
