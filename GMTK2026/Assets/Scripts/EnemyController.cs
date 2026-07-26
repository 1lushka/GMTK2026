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
    [SerializeField] private ImpulseReceiver impulseReceiver;

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
    private bool comboStunned;
    private bool impulseFlying;
    private float lastAttackTime = -Mathf.Infinity;
    private Coroutine knockbackCoroutine;
    private Coroutine hurtCoroutine;
    private Quaternion standingVisualRotation;
    private bool controlsFlightVisual;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (health == null) health = GetComponent<HealthComponent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (impulseReceiver == null) impulseReceiver = GetComponent<ImpulseReceiver>();
        if (animator != null) standingVisualRotation = animator.transform.localRotation;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        agent.stoppingDistance = stoppingDistance;

        if (health != null)
        {
            health.onDamaged.AddListener(OnDamaged);
            health.onDeath.AddListener(OnDeath);
        }

        // Инициализация партиклов
        if (hurtParticles != null) hurtParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (idleParticles != null) idleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        PlayParticle(idleParticles);
    }

    private void Update()
    {
        UpdateImpulseState();
        if (comboStunned || impulseFlying || isKnockedBack || isHurt ||
            (health != null && health.CurrentHealth <= 0)) return;

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

    private void LateUpdate()
    {
        if (animator == null || impulseReceiver == null) return;

        Vector2 velocity = impulseReceiver.CurrentVelocity;
        if (impulseReceiver.IsMoving && velocity.sqrMagnitude > 0.0001f)
        {
            Vector3 movement = new Vector3(velocity.x, 0f, velocity.y).normalized;
            animator.transform.rotation = Quaternion.LookRotation(-movement, Vector3.up);
            controlsFlightVisual = true;
        }
        else if (controlsFlightVisual)
        {
            animator.transform.localRotation = standingVisualRotation;
            controlsFlightVisual = false;
        }
    }

    public void SetComboStunned(bool stunned)
    {
        if (comboStunned == stunned) return;

        comboStunned = stunned;
        if (stunned)
            PlayHurtFeedback();
        UpdateAiLock();
    }

    private void UpdateImpulseState()
    {
        bool isFlying = impulseReceiver != null && (impulseReceiver.IsMoving || impulseReceiver.IsAirborne);
        if (impulseFlying == isFlying) return;

        impulseFlying = isFlying;
        if (isFlying)
            PlayHurtFeedback();
        UpdateAiLock();
    }

    private void UpdateAiLock()
    {
        bool locked = comboStunned || impulseFlying;
        if (health != null)
            health.SetStunned(locked);
        if (locked)
        {
            StopControlCoroutines();
            if (agent.enabled)
                agent.enabled = false;
            if (animator != null)
                animator.SetFloat(speedParamName, 0f);
            return;
        }

        if (health != null && health.CurrentHealth > 0 && !agent.enabled)
            agent.enabled = true;
        isHurt = false;
        isKnockedBack = false;
        PlayParticle(idleParticles);
    }

    private void StopControlCoroutines()
    {
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }
        if (hurtCoroutine != null)
        {
            StopCoroutine(hurtCoroutine);
            hurtCoroutine = null;
        }

        isKnockedBack = false;
        isHurt = true;
    }

    private void PlayHurtFeedback()
    {
        if (animator != null)
            animator.SetTrigger(hurtTriggerName);
        StopParticle(idleParticles);
        PlayParticle(hurtParticles);
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
        lastAttackTime = Time.time;

        if (animator != null)
            animator.SetTrigger(attackTriggerName);

        if (ForgettingBoxer.Knockout.KnockoutAPI.TakeDamage(attackDamage)) return;

        HealthComponent playerHealth = player.GetComponentInChildren<HealthComponent>();
        if (playerHealth != null)
            playerHealth.TakeDamage(attackDamage);
    }

    public void ApplyImpulse(Vector3 force)
    {
        if (isKnockedBack) return;
        Alert();

        // Останавливаем текущий стан, если есть
        if (hurtCoroutine != null)
        {
            StopCoroutine(hurtCoroutine);
            hurtCoroutine = null;
            isHurt = false;
            agent.isStopped = false;
            PlayParticle(idleParticles); // возвращаем idle
        }

        knockbackCoroutine = StartCoroutine(KnockbackRoutine(force));
    }

    private IEnumerator KnockbackRoutine(Vector3 force)
    {
        isKnockedBack = true;
        agent.enabled = false;
        rb.isKinematic = false;
        rb.AddForce(force, ForceMode.Impulse);

        if (animator != null)
            animator.SetFloat(speedParamName, 0f);

        StopParticle(idleParticles);
        StopParticle(hurtParticles);

        yield return new WaitForSeconds(knockbackDuration);

        // Сбрасываем скорость перед возвратом к агенту
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        if (!comboStunned && !impulseFlying)
            agent.enabled = true;
        isKnockedBack = false;
        knockbackCoroutine = null;

        // Если всё ещё жив, включаем idle
        if (health.CurrentHealth > 0)
            PlayParticle(idleParticles);
    }

    private void OnDamaged(int damage)
    {
        Alert();
        if (comboStunned || impulseFlying)
        {
            if (animator != null)
                animator.SetTrigger(hurtTriggerName);
            return;
        }
        // Если уже в нокбэке, не запускаем стан (урон всё равно нанесён)
        if (!isKnockedBack && !isHurt)
        {
            // Останавливаем предыдущий стан, если был (на всякий случай)
            if (hurtCoroutine != null)
                StopCoroutine(hurtCoroutine);
            hurtCoroutine = StartCoroutine(HurtRoutine());
        }
    }

    private IEnumerator HurtRoutine()
    {
        isHurt = true;
        agent.isStopped = true;

        if (animator != null)
            animator.SetTrigger(hurtTriggerName);

        StopParticle(idleParticles);
        PlayParticle(hurtParticles);

        yield return new WaitForSeconds(hurtStunDuration);

        StopParticle(hurtParticles);

        // Восстанавливаем движение, только если за время стана не были убиты или отброшены
        if (health != null && health.CurrentHealth > 0 && !isKnockedBack)
        {
            agent.isStopped = false;
            PlayParticle(idleParticles);
        }
        isHurt = false;
        hurtCoroutine = null;
    }

    private void OnDeath()
    {
        // Гарантированно сбрасываем все состояния
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
        isKnockedBack = false;
        isHurt = false;

        agent.enabled = false;
        rb.isKinematic = false;
        enabled = false;

        if (animator != null) animator.SetFloat(speedParamName, 0f);

        StopParticle(idleParticles);
        StopParticle(hurtParticles);

        Destroy(gameObject, 0.5f);
    }

    private void OnDestroy()
    {
        if (health == null) return;
        health.onDamaged.RemoveListener(OnDamaged);
        health.onDeath.RemoveListener(OnDeath);
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
