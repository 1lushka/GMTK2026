using UnityEngine;

public abstract class ActiveAbility : MonoBehaviour
{
    [Header("Ability")]
    [SerializeField] protected AbilityDefinition abilityDefinition;
    [SerializeField] protected KeyCode activationKey = KeyCode.Space;

    [Header("Cooldown")]
    [SerializeField] protected float cooldown = 0.5f;
    protected float cooldownTimer;

    protected PlayerAbilityManager abilityManager;
    protected TopDownController controller;
    protected Rigidbody rb;                              // <-- добавили

    protected virtual void Start()
    {
        abilityManager = GetComponent<PlayerAbilityManager>();
        controller = GetComponent<TopDownController>();
        rb = GetComponent<Rigidbody>();                  // <-- инициализируем

        if (abilityManager == null)
            Debug.LogError($"{GetType().Name}: PlayerAbilityManager not found on player!");
        if (controller == null)
            Debug.LogError($"{GetType().Name}: TopDownController not found on player!");
        if (rb == null)
            Debug.LogError($"{GetType().Name}: Rigidbody not found on player!");
    }

    protected virtual void Update()
    {
        if (abilityManager == null || controller == null) return;

        if (!abilityManager.HasAbility(abilityDefinition)) return;

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (Input.GetKeyDown(activationKey) && CanActivate())
        {
            abilityManager.NotifyAbilityActivated(this);
            Activate();
            cooldownTimer = cooldown;
        }
    }

    protected virtual bool CanActivate() => true;
    protected abstract void Activate();
}
