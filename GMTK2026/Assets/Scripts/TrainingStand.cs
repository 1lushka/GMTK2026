using UnityEngine;

public class TrainingStand : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private HealthComponent health;
    [SerializeField] private Rigidbody rb;

    [Header("Spin")]
    [SerializeField] private float playerSpinAngle = 180f;
    [SerializeField, Min(0.01f)] private float playerSpinDuration = 0.3f;
    [SerializeField] private float minPlayerSpeed = 3f;
    [SerializeField] private float damageSpinMultiplier = 15f;
    [SerializeField] private float maxSpinAngle = 720f;
    [SerializeField] private float impulseTorqueMultiplier = 500f;

    [Header("Impact During Spin")]
    [SerializeField] private float spinThreshold = 30f;
    [SerializeField] private int damageToEnemies = 1;
    [SerializeField] private float pushForce = 10f;

    private float spinTimeRemaining;
    private float spinDirection = 1f;
    private float originalAngularDamping;
    private bool IsSpinActive => spinTimeRemaining > 0f;

    private void Awake()
    {
        if (health == null) health = GetComponent<HealthComponent>();
        if (rb == null) rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezePosition |
            RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        originalAngularDamping = rb.angularDamping;

        if (health != null)
        {
            health.onDamaged.AddListener(OnDamaged);
            health.onDeath.AddListener(OnDeath);
        }
    }

    private void FixedUpdate()
    {
        if (spinTimeRemaining <= 0f) return;

        spinTimeRemaining -= Time.fixedDeltaTime;
        if (spinTimeRemaining <= 0f)
            rb.angularDamping = originalAngularDamping;
    }

    public void OnArmCollision(Collider other, Vector3 contactPoint, Vector3 relativeVelocity)
    {
        ImpulseReceiver receiver = other.GetComponentInParent<ImpulseReceiver>();
        TopDownController player = other.GetComponentInParent<TopDownController>();
        Vector3 incoming = receiver != null
            ? new Vector3(receiver.CurrentVelocity.x, 0f, receiver.CurrentVelocity.y)
            : Vector3.zero;
        if (player != null && player.CurrentVelocity.sqrMagnitude > incoming.sqrMagnitude)
            incoming = player.CurrentVelocity;
        if (incoming.sqrMagnitude < relativeVelocity.sqrMagnitude)
            incoming = relativeVelocity;

        bool flyingReceiver = receiver != null && receiver.CurrentState == ImpulseReceiver.State.Flying &&
            receiver.LastImpulseSource != gameObject && incoming.magnitude >= minPlayerSpeed;
        if (!IsSpinActive && (player != null || flyingReceiver))
        {
            StartSpin(contactPoint, incoming, Mathf.Max(incoming.magnitude, minPlayerSpeed));
        }

        if (Mathf.Abs(rb.angularVelocity.y) * Mathf.Rad2Deg <= spinThreshold) return;

        HealthComponent targetHealth = other.GetComponentInParent<HealthComponent>();
        if (targetHealth != null && targetHealth != health)
            targetHealth.TakeDamage(damageToEnemies);

        ApplyPush(receiver, contactPoint);
    }

    public void ApplyImpulseSpin(float force, Vector3 sourcePosition)
    {
        if (IsSpinActive)
        {
            RefreshSpinTime();
            return;
        }

        Vector3 sourceOffset = sourcePosition - transform.position;
        float direction = Mathf.Sign(Vector3.Cross(transform.forward, sourceOffset).y);
        if (Mathf.Approximately(direction, 0f)) direction = spinDirection;
        AddSpinSpeed(force * impulseTorqueMultiplier, direction);
    }

    private void OnDamaged(int damage)
    {
        if (IsSpinActive)
        {
            RefreshSpinTime();
            EnsureImpactSpinSpeed();
            return;
        }

        AddSpinSpeed(Mathf.Max(playerSpinAngle, damage * damageSpinMultiplier), spinDirection);
    }

    public void KeepSpinningFromCombo()
    {
        if (IsSpinActive)
        {
            RefreshSpinTime();
            EnsureImpactSpinSpeed();
        }
        else
        {
            AddSpinSpeed(playerSpinAngle, spinDirection);
        }
    }

    private void StartSpin(Vector3 contactPoint, Vector3 incomingVelocity, float force)
    {
        Vector3 radius = contactPoint - transform.position;
        radius.y = 0f;
        float direction = Mathf.Sign(Vector3.Cross(radius, incomingVelocity).y);
        if (Mathf.Approximately(direction, 0f)) direction = spinDirection;
        AddSpinSpeed(Mathf.Max(playerSpinAngle, force * impulseTorqueMultiplier), direction);
    }

    private void AddSpinSpeed(float angle, float direction)
    {
        spinDirection = direction;
        spinTimeRemaining = playerSpinDuration;
        rb.angularDamping = 0f;

        float duration = Mathf.Max(0.01f, playerSpinDuration);
        float maxSpeed = maxSpinAngle / duration;
        float addedSpeed = Mathf.Abs(angle) / duration;
        float currentSpeed = Mathf.Abs(rb.angularVelocity.y) * Mathf.Rad2Deg;
        float speed = Mathf.Min(currentSpeed + addedSpeed, maxSpeed);
        rb.angularVelocity = Vector3.up * (speed * Mathf.Deg2Rad * spinDirection);
    }

    private void RefreshSpinTime()
    {
        spinTimeRemaining = playerSpinDuration;
        rb.angularDamping = 0f;
    }

    private void EnsureImpactSpinSpeed()
    {
        float duration = Mathf.Max(0.01f, playerSpinDuration);
        float targetSpeed = Mathf.Min(playerSpinAngle / duration, maxSpinAngle / duration);
        float currentSpeed = Mathf.Abs(rb.angularVelocity.y) * Mathf.Rad2Deg;
        if (currentSpeed >= targetSpeed) return;

        rb.angularVelocity = Vector3.up * (targetSpeed * Mathf.Deg2Rad * spinDirection);
    }

    private void ApplyPush(ImpulseReceiver target, Vector3 contactPoint)
    {
        if (target == null || target.LastImpulseSource == gameObject) return;

        Vector3 radius = contactPoint - transform.position;
        radius.y = 0f;
        Vector3 tangent = Vector3.Cross(Vector3.up * spinDirection, radius.normalized);
        target.ForceApplyImpulse(new Vector2(tangent.x, tangent.z), pushForce, gameObject);
    }

    private void OnDeath()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (health == null) return;
        health.onDamaged.RemoveListener(OnDamaged);
        health.onDeath.RemoveListener(OnDeath);
    }
}
