using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashAbility : ActiveAbility
{
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float windUpTime = 0.2f;

    [Header("Dash Impact")]
    [SerializeField, Min(0f)] private float hitRadius = 0.6f;
    [SerializeField, Min(0f)] private float impulseForce = 10f;
    [SerializeField, Min(0)] private int damage = 1;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Animation")]
    [SerializeField] private string dashTrigger = "Dash";
    [SerializeField] private bool useAnimator = true;

    [Header("Particles")]
    [SerializeField] private ParticleSystem dashParticle;

    private Animator animator;
    private Coroutine dashRoutine;
    private bool originalDetectCollisions;
    private readonly Collider[] hitBuffer = new Collider[32];
    private readonly HashSet<ImpulseReceiver> hitReceivers = new HashSet<ImpulseReceiver>();
    private readonly HashSet<HealthComponent> damagedTargets = new HashSet<HealthComponent>();

    public bool IsDashing { get; private set; }

    protected override void Start()
    {
        base.Start();

        if (useAnimator && controller != null)
        {
            animator = controller.Animator;
            if (animator == null)
                Debug.LogWarning("DashAbility: Animator not found in TopDownController!");
        }
    }

    protected override void Activate()
    {
        dashRoutine = StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        if (animator != null)
            animator.SetTrigger(dashTrigger);

        yield return new WaitForSeconds(windUpTime);

        if (dashParticle != null)
        {
            dashParticle.Clear();
            dashParticle.Play();
        }

        Vector3 dashDirection = GetDashDirection();
        Vector3 dashRight = Vector3.Cross(Vector3.up, dashDirection).normalized;
        hitReceivers.Clear();
        damagedTargets.Clear();
        BeginDashProtection();
        controller.enableMovement = false;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            Vector3 newPos = rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime;
            ApplyDashImpact(rb.position, newPos, dashRight);
            rb.MovePosition(newPos);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        EndDash();
    }

    private void ApplyDashImpact(Vector3 start, Vector3 end, Vector3 dashRight)
    {
        int hitCount = Physics.OverlapCapsuleNonAlloc(start, end, hitRadius, hitBuffer,
            hitMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null || hit.transform.root == transform.root) continue;

            DamageTarget(hit);
            PushTarget(hit, dashRight, end);
        }
    }

    private void DamageTarget(Collider hit)
    {
        if (damage <= 0) return;

        HealthComponent health = hit.GetComponentInParent<HealthComponent>();
        if (health == null || !damagedTargets.Add(health)) return;

        health.TakeDamage(damage);
    }

    private void PushTarget(Collider hit, Vector3 dashRight, Vector3 playerPosition)
    {
        if (impulseForce <= 0f) return;

        ImpulseReceiver receiver = hit.GetComponentInParent<ImpulseReceiver>();
        if (receiver == null || !hitReceivers.Add(receiver)) return;

        Vector3 offset = receiver.transform.position - playerPosition;
        float side = Mathf.Sign(Vector3.Dot(offset, dashRight));
        if (Mathf.Approximately(side, 0f)) side = 1f;

        Vector3 pushDirection = dashRight * side;
        receiver.ApplyImpulse(new Vector2(pushDirection.x, pushDirection.z), impulseForce, gameObject);
    }

    private Vector3 GetDashDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        if (inputDir.magnitude > 0.1f)
            return inputDir;
        else
            return transform.forward;
    }

    private void BeginDashProtection()
    {
        IsDashing = true;
        originalDetectCollisions = rb.detectCollisions;
        rb.detectCollisions = false;
    }

    private void EndDash()
    {
        if (!IsDashing) return;

        IsDashing = false;
        rb.detectCollisions = originalDetectCollisions;
        controller.SetVelocity(Vector3.zero);
        controller.enableMovement = true;
        dashRoutine = null;
    }

    private void OnDisable()
    {
        if (dashRoutine != null)
            StopCoroutine(dashRoutine);
        dashRoutine = null;
        EndDash();
    }
}
