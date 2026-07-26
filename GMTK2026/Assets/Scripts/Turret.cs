using UnityEngine;

public class Turret : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private HealthComponent health;
    [SerializeField] private Transform turretBase;      
    [SerializeField] private Animator turretAnimator;     

    [Header("Shooting")]
    [SerializeField] private GameObject shurikenPrefab;
    [SerializeField] private float fireInterval = 1.5f;
    [SerializeField] private float shurikenSpeed = 12f;
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private Transform firePoint;

    [Header("Rotation")]
    [SerializeField] private bool rotateTowardsPlayer = true;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Animation")]
    [SerializeField] private string fireTriggerName = "Fire";

    private Transform player;
    private float timer;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (health == null) health = GetComponent<HealthComponent>();

        if (health != null)
        {
            health.onDamaged.AddListener(OnDamaged);
            health.onDeath.AddListener(OnDeath);
        }

        if (firePoint == null) firePoint = transform;
        timer = fireInterval;
    }

    private void Update()
    {
        if (player == null || health.CurrentHealth <= 0) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange) return;

        if (rotateTowardsPlayer && turretBase != null)
        {
            Vector3 direction = (player.position - turretBase.position).normalized;
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                turretBase.rotation = Quaternion.Slerp(turretBase.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Fire();
            timer = fireInterval;
        }
    }

    private void Fire()
    {
        if (shurikenPrefab == null) return;

        Vector3 spawnPos = firePoint.position;
        Vector3 direction = Vector3.zero;
        direction = -firePoint.forward;
        direction.y = 0f;

        GameObject shurikenObj = Instantiate(shurikenPrefab, spawnPos, Quaternion.LookRotation(direction));
        Shuriken shuriken = shurikenObj.GetComponent<Shuriken>();
        if (shuriken != null)
        {
            shuriken.Initialize(direction * shurikenSpeed, gameObject);
        }
        else
        {
            Rigidbody rb = shurikenObj.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = direction * shurikenSpeed;
        }

        if (turretAnimator != null)
            turretAnimator.SetTrigger(fireTriggerName);
    }

    private void OnDamaged(int damage) { }

    private void OnDeath()
    {
        GetComponent<Collider>().enabled = false;
        enabled = false;
        Destroy(gameObject, 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}