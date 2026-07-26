using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private HealthComponent health;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float stoppingDistance = 0.2f;

    [Header("Attack")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Animation")]
    [SerializeField] private string speedParamName = "Speed";
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string hurtTriggerName = "Hurt";
    [SerializeField] private float animationMoveThreshold = 0.1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.4f;

    [Header("Hurt")]
    [SerializeField] private float hurtStunDuration = 0.3f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem idleParticles;
    [SerializeField] private ParticleSystem hurtParticles;

    private Transform player;
    private bool isAlerted;
    private bool isKnockedBack;
    private bool isHurt;
    private float lastAttackTime = -Mathf.Infinity;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (health == null) health = GetComponent<HealthComponent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        agent.stoppingDistance = stoppingDistance;

        if (health != null)
        {
            health.onDamaged.AddListener(OnDamaged);
            health.onDeath.AddListener(OnDeath);
        }

        // Явно выключаем все партиклы на старте, затем включаем idle
        if (hurtParticles != null) hurtParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (idleParticles != null) idleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        PlayParticle(idleParticles);
    }

    private void Update()
    {
        if (isKnockedBack || isHurt || (health != null && health.CurrentHealth <= 0)) return;

        UpdateAnimation();

        if (isAlerted)
        {
            agent.SetDestination(player.position);
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
    }

    public void Alert()
    {
        if (!isAlerted)
        {
            isAlerted = true;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;
        bool isMoving = agent.enabled && agent.velocity.magnitude > animationMoveThreshold;
        animator.SetFloat(speedParamName, isMoving ? 1f : 0f);
    }

    private void Attack()
    {
        if (animator != null)
            animator.SetTrigger(attackTriggerName);

        HealthComponent playerHealth = player.GetComponent<HealthComponent>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            lastAttackTime = Time.time;
        }
    }

    public void ApplyImpulse(Vector3 force)
    {
        if (isKnockedBack) return;
        Alert();
        StartCoroutine(KnockbackRoutine(force));
    }

    private IEnumerator KnockbackRoutine(Vector3 force)
    {
        isKnockedBack = true;
        agent.enabled = false;
        rb.isKinematic = false;
        rb.AddForce(force, ForceMode.Impulse);

        if (animator != null)
            animator.SetFloat(speedParamName, 0f);

        // Выключаем idle на время отбрасывания
        StopParticle(idleParticles);

        yield return new WaitForSeconds(knockbackDuration);

        rb.isKinematic = true;
        agent.enabled = true;
        isKnockedBack = false;

        // Включаем idle обратно
        PlayParticle(idleParticles);
    }

    private void OnDamaged(int damage)
    {
        Alert();
        if (!isKnockedBack && !isHurt)
        {
            StartCoroutine(HurtRoutine());
        }
    }

    private IEnumerator HurtRoutine()
    {
        isHurt = true;
        agent.isStopped = true;

        if (animator != null)
            animator.SetTrigger(hurtTriggerName);

        // Переключаемся на hurt-партиклы
        StopParticle(idleParticles);
        PlayParticle(hurtParticles);

        yield return new WaitForSeconds(hurtStunDuration);

        // Выключаем hurt-партиклы
        StopParticle(hurtParticles);

        if (health != null && health.CurrentHealth > 0 && !isKnockedBack)
        {
            agent.isStopped = false;
            // Возвращаем idle
            PlayParticle(idleParticles);
        }
        isHurt = false;
    }

    private void OnDeath()
    {
        agent.enabled = false;
        rb.isKinematic = false;
        enabled = false;
        if (animator != null) animator.SetFloat(speedParamName, 0f);

        // Останавливаем все партиклы
        StopParticle(idleParticles);
        StopParticle(hurtParticles);

        Destroy(gameObject, 0.5f);
    }

    private void PlayParticle(ParticleSystem ps)
    {
        if (ps != null)
        {
            if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }
    }

    private void StopParticle(ParticleSystem ps)
    {
        if (ps != null && ps.isPlaying)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}