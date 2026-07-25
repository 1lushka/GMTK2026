using System.Collections;
using UnityEngine;

public class TrainingStand : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private HealthComponent health;
    [SerializeField] private Rigidbody rb; // назначить в инспекторе или найдётся

    [Header("Player Collision")]
    [SerializeField] private float playerSpinAngle = 180f;
    [SerializeField] private float playerSpinDuration = 0.3f;
    [SerializeField] private float minPlayerSpeed = 3f;

    [Header("Damage & Impulse")]
    [SerializeField] private float damageSpinMultiplier = 15f;
    [SerializeField] private float maxSpinAngle = 720f;
    [SerializeField] private float impulseTorqueMultiplier = 500f; // для преобразования силы импульса в крутящий момент

    [Header("Impact During Spin")]
    [SerializeField] private float spinThreshold = 30f;   // угловая скорость (град/с), выше которой наносится урон
    [SerializeField] private int damageToEnemies = 1;
    [SerializeField] private float pushForce = 10f;

    private bool playerSpinActive;
    private Coroutine playerSpinCoroutine;
    private float originalAngularDrag;

    private void Awake()
    {
        if (health == null) health = GetComponent<HealthComponent>();
        if (rb == null) rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        originalAngularDrag = rb.angularDamping;

        if (health != null)
        {
            health.onDamaged.AddListener(OnDamaged);
            health.onDeath.AddListener(OnDeath);
        }
    }

    // Вызывается из TrainingStandArm при OnCollisionEnter
    public void OnArmCollision(Collider other, Vector3 contactPoint)
    {
        // Если столкнулись с игроком и ещё не запущено принудительное вращение от игрока
        if (other.CompareTag("Player") && !playerSpinActive)
        {
            TopDownController controller = other.GetComponentInParent<TopDownController>();
            if (controller != null)
            {
                Vector3 playerVelocity = controller.CurrentVelocity;
                float speed = playerVelocity.magnitude;
                if (speed >= minPlayerSpeed)
                {
                    Vector3 dirToContact = (contactPoint - transform.position).normalized;
                    dirToContact.y = 0f;
                    float dot = Vector3.Dot(dirToContact, playerVelocity.normalized);
                    if (Mathf.Abs(dot) < 0.3f) return; // касательный удар

                    Vector3 cross = Vector3.Cross(dirToContact, playerVelocity.normalized);
                    float sign = Mathf.Sign(cross.y);
                    if (Mathf.Approximately(sign, 0f)) sign = 1f;

                    float targetAngularSpeed = Mathf.Deg2Rad * playerSpinAngle / Mathf.Max(playerSpinDuration, 0.01f) * sign;
                    playerSpinCoroutine = StartCoroutine(PlayerSpinRoutine(targetAngularSpeed, playerSpinDuration));
                }
            }

            // Отбрасываем игрока независимо от того, запустили вращение или нет (физика могла уже сработать)
            Rigidbody playerRb = other.attachedRigidbody;
            if (playerRb == null) playerRb = other.GetComponentInParent<Rigidbody>();
            //ApplyPush(playerRb, contactPoint);
        }
        // Во время вращения (своя угловая скорость или от физики) наносим урон и отбрасываем
        else if (Mathf.Abs(rb.angularVelocity.y) * Mathf.Rad2Deg > spinThreshold)
        {
            if (other.CompareTag("Player"))
            {
                Rigidbody playerRb = other.attachedRigidbody;
                if (playerRb == null) playerRb = other.GetComponentInParent<Rigidbody>();
                ApplyPush(playerRb, contactPoint);
            }
            else
            {
                HealthComponent targetHealth = other.GetComponentInParent<HealthComponent>();
                if (targetHealth != null)
                    targetHealth.TakeDamage(damageToEnemies);

                Rigidbody targetRb = other.attachedRigidbody;
                if (targetRb == null) targetRb = other.GetComponentInParent<Rigidbody>();
                ApplyPush(targetRb, contactPoint);
            }
        }
    }

    // Вызывается при получении урона (из HealthComponent)
    private void OnDamaged(int damage)
    {
        float angle = Mathf.Clamp(damage * damageSpinMultiplier, 0f, maxSpinAngle);
        float sign = Random.value > 0.5f ? 1f : -1f;
        rb.AddTorque(0, angle * Mathf.Deg2Rad * sign, 0, ForceMode.Impulse);
    }

    // Вызывается извне (например, взрывным снарядом) для добавления вращения без урона
    public void ApplyImpulseSpin(float force, Vector3 sourcePosition)
    {
        Vector3 dirToSource = (sourcePosition - transform.position).normalized;
        Vector3 cross = Vector3.Cross(transform.forward, dirToSource);
        float sign = cross.y > 0 ? 1f : -1f;
        float torque = force * impulseTorqueMultiplier * sign;
        rb.AddTorque(0, torque * Mathf.Deg2Rad, 0, ForceMode.Impulse);
    }

    // Корутина временного отключения трения и задания постоянной угловой скорости для игрока
    private IEnumerator PlayerSpinRoutine(float targetAngularSpeed, float duration)
    {
        playerSpinActive = true;
        rb.angularDamping = 0f;
        rb.angularVelocity = new Vector3(0, targetAngularSpeed, 0);

        yield return new WaitForSeconds(duration);

        rb.angularDamping = originalAngularDrag;
        playerSpinActive = false;
        // угловую скорость не обнуляем, пусть трение само гасит
    }

    private void ApplyPush(Rigidbody targetRb, Vector3 contactPoint)
    {
        if (targetRb != null && !targetRb.isKinematic)
        {
            Vector3 pushDir = (targetRb.position - transform.position).normalized;
            pushDir.y = 0f;
            targetRb.AddForce(pushDir * pushForce, ForceMode.Impulse);
        }
    }

    private void OnDeath()
    {
        Destroy(gameObject);
    }
}