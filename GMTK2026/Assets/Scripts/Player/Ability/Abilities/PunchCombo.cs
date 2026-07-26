using UnityEngine;

public class PunchComboAbility : ActiveAbility
{
    private Animator animator;

    [Header("Animation")]
    [SerializeField] private string comboBool = "Combo";
    [SerializeField] private bool useAnimator = true;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Attack")]
    [SerializeField] private float pushForce = 10f;
    [SerializeField] private Transform punchCollider;       

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageInterval = 0.5f;   

    private bool isPunching = false;
    private Collider punchColliderComponent;
    private float lastDamageTime;

    public bool IsPunching => isPunching;

    protected override void Activate()
    {
        if (!CanActivate()) return;
        cooldownTimer = cooldown;
        animator.SetBool(comboBool, true);
        isPunching = true;
        controller.ActivateRotation(false);
        punchCollider.gameObject.SetActive(true);

        Vector3 direction = LookAtMouse();
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(targetRotation);

        lastDamageTime = Time.time - damageInterval;
    }

    protected void Deactivate()
    {
        animator.SetBool(comboBool, false);
        isPunching = false;
        controller.ActivateRotation(true);
        punchCollider.gameObject.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();

        if (useAnimator && controller != null)
        {
            animator = controller.Animator;
            if (animator == null)
                Debug.LogWarning("PunchComboAbility: Animator not found in TopDownController!");
        }

        if (punchCollider != null)
            punchColliderComponent = punchCollider.GetComponent<Collider>();
        else
            Debug.LogError("PunchCollider Transform is not assigned!");
    }

    protected override void Update()
    {
        if (!isPunching && cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // Ввод
        if (Input.GetKeyDown(activationKey))
            Activate();
        if (Input.GetKeyUp(activationKey))
            Deactivate();

        if (isPunching)
        {
            // Поворот к мыши и движение вперёд
            Vector3 direction = LookAtMouse();
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion smoothRotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(smoothRotation);
            ApplyPush(-direction);

            // Нанесение урона по таймеру
            if (Time.time >= lastDamageTime + damageInterval)
            {
                DealDamageToAllInZone();
                lastDamageTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Собирает все объекты в зоне punchCollider и наносит им урон.
    /// </summary>
    private void DealDamageToAllInZone()
    {
        if (punchColliderComponent == null)
            return;

        Collider[] hits = null;

        if (punchColliderComponent is BoxCollider box)
        {
            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
            hits = Physics.OverlapBox(center, halfExtents, box.transform.rotation);
        }
        else if (punchColliderComponent is SphereCollider sphere)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            float radius = sphere.radius * Mathf.Max(sphere.transform.lossyScale.x,
                Mathf.Max(sphere.transform.lossyScale.y, sphere.transform.lossyScale.z));
            hits = Physics.OverlapSphere(center, radius);
        }
        else if (punchColliderComponent is CapsuleCollider capsule)
        {
            Vector3 capsuleCenter = capsule.transform.TransformPoint(capsule.center);
            float height = capsule.height * capsule.transform.lossyScale.y;
            float radius = capsule.radius * Mathf.Max(capsule.transform.lossyScale.x, capsule.transform.lossyScale.z);

            // Определяем направление капсулы (0=X, 1=Y, 2=Z)
            Vector3 direction = Vector3.up; // по умолчанию Y
            if (capsule.direction == 0) direction = capsule.transform.right;
            else if (capsule.direction == 1) direction = capsule.transform.up;
            else if (capsule.direction == 2) direction = capsule.transform.forward;

            float halfHeight = Mathf.Max(0, height * 0.5f - radius);
            Vector3 point1 = capsuleCenter + direction * halfHeight;
            Vector3 point2 = capsuleCenter - direction * halfHeight;

            hits = Physics.OverlapCapsule(point1, point2, radius);
        }
        else
        {
            Debug.LogError("PunchCollider must have BoxCollider, SphereCollider or CapsuleCollider!");
            return;
        }

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform))
                continue;

            HealthComponent health = hit.GetComponentInParent<HealthComponent>();
            if (health != null && !health.CompareTag("Player"))
            {
                health.TakeDamage(damage);
                Debug.Log($"Punch dealt {damage} damage to {hit.name}");
            }

            Rigidbody hitRb = hit.attachedRigidbody;
            if (hitRb != null && !hitRb.isKinematic)
            {
                Vector3 pushDir = (hitRb.position - transform.position).normalized;
                pushDir.y = 0f;
                hitRb.AddForce(pushDir * pushForce * 0.8f, ForceMode.Impulse);
            }
        }
    }
    Vector3 LookAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(distance);
            Vector3 direction = mouseWorldPos - transform.position;
            direction.y = 0f;
            return direction;
        }
        return Vector3.zero;
    }

    private void ApplyPush(Vector3 direction)
    {
        if (rb != null && !rb.isKinematic)
        {
            Vector3 pushDir = direction.normalized;
            pushDir.y = 0f;
            rb.MovePosition(rb.position + pushDir * pushForce * Time.fixedDeltaTime);
        }
    }

    protected override bool CanActivate()
    {
        return cooldownTimer <= 0;
    }
}