using UnityEngine;
using ForgettingBoxer.Knockout;

public sealed class MagicGrabAbility : ActiveAbility
{
    private enum GrabState
    {
        Idle,
        ProjectileFlying,
        TargetOrbitingPlayer,
        PlayerOrbitingAnchor
    }

    [Header("Glove Projectile")]
    [SerializeField] private MagicGrabProjectile glovePrefab;
    [SerializeField] private float gloveProjectileSpeed = 15f;
    [SerializeField] private float gloveMaxDistance = 12f;
    [SerializeField] private float gloveSpawnDistance = 0.6f;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Orbit")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float collisionImpulseForce = 8f;
    [SerializeField] private float releaseImpulseForce = 10f;

    [Header("Optional Visual Hook")]
    [SerializeField] private Transform linkOrigin;

    private readonly RaycastHit[] orbitHits = new RaycastHit[12];

    private GrabState state;
    private MagicGrabProjectile projectile;
    private ImpulseReceiver targetReceiver;
    private Rigidbody targetBody;
    private Transform anchorTransform;
    private Vector3 anchorLocalPoint;
    private Vector3 orbitOffset;
    private Vector3 lastMotionDirection;
    private ImpulseReceiver lastImpulsedReceiver;
    private float nextCollisionImpulseTime;
    private HealthComponent health;
    private KnockoutSystem knockoutSystem;
    private Camera mainCamera;

    public bool IsActive => state != GrabState.Idle;
    public Transform LinkOrigin => linkOrigin != null ? linkOrigin : transform;
    public Vector3 LinkTarget => GetAnchorPosition();

    protected override void Start()
    {
        base.Start();
        mainCamera = Camera.main;
        health = GetComponent<HealthComponent>();
        knockoutSystem = KnockoutSystem.Instance;
        SubscribeToInterruptions();
    }

    private void OnEnable()
    {
        if (abilityManager != null)
            SubscribeToInterruptions();
    }

    protected override void Update()
    {
        if (state != GrabState.Idle && Input.GetKeyDown(activationKey))
        {
            CancelGrab(true);
            return;
        }

        base.Update();

        if (state != GrabState.Idle && anchorTransform == null && state != GrabState.ProjectileFlying)
            CancelGrab(false);
    }

    private void FixedUpdate()
    {
        if (state == GrabState.TargetOrbitingPlayer)
            OrbitTargetAroundPlayer();
        else if (state == GrabState.PlayerOrbitingAnchor)
            OrbitPlayerAroundAnchor();
    }

    protected override void Activate()
    {
        if (glovePrefab == null) return;

        Vector3 direction = GetAimDirection();
        Vector3 spawnPosition = transform.position + direction * gloveSpawnDistance;
        projectile = Instantiate(glovePrefab, spawnPosition, Quaternion.LookRotation(direction));
        projectile.Initialize(gameObject, direction, gloveProjectileSpeed, gloveMaxDistance,
            collisionRadius, hitMask, OnProjectileHit, OnProjectileMissed);
        state = GrabState.ProjectileFlying;
    }

    private void OnProjectileHit(RaycastHit hit)
    {
        projectile = null;
        ImpulseReceiver receiver = hit.collider.GetComponentInParent<ImpulseReceiver>();

        if (receiver != null && receiver.BeginExternalControl())
        {
            BeginTargetOrbit(receiver);
            return;
        }

        BeginPlayerOrbit(hit.collider.transform, hit.point);
    }

    private void BeginTargetOrbit(ImpulseReceiver receiver)
    {
        targetReceiver = receiver;
        targetBody = receiver.GetComponent<Rigidbody>();
        anchorTransform = receiver.transform;
        orbitOffset = targetBody.position - rb.position;
        orbitOffset.y = 0f;
        state = GrabState.TargetOrbitingPlayer;
    }

    private void BeginPlayerOrbit(Transform anchor, Vector3 worldPoint)
    {
        anchorTransform = anchor;
        anchorLocalPoint = anchor.InverseTransformPoint(worldPoint);
        orbitOffset = rb.position - worldPoint;
        orbitOffset.y = 0f;
        controller.enableMovement = false;
        state = GrabState.PlayerOrbitingAnchor;
    }

    private void OrbitTargetAroundPlayer()
    {
        if (targetBody == null)
        {
            CancelGrab(false);
            return;
        }

        Vector3 desiredOffset = RotateClockwise(orbitOffset);
        Vector3 movement = rb.position + desiredOffset - targetBody.position;
        if (HandleOrbitCollisions(targetBody.position, movement, targetReceiver))
        {
            SwitchToPlayerOrbit();
            return;
        }

        targetBody.MovePosition(targetBody.position + movement);
        orbitOffset = desiredOffset;
        RememberMotion(movement);
    }

    private void OrbitPlayerAroundAnchor()
    {
        Vector3 anchorPosition = GetAnchorPosition();
        Vector3 desiredOffset = RotateClockwise(orbitOffset);
        Vector3 movement = anchorPosition + desiredOffset - rb.position;
        rb.MovePosition(rb.position + movement);
        orbitOffset = desiredOffset;
        RememberMotion(movement);
    }

    private bool HandleOrbitCollisions(Vector3 origin, Vector3 movement, ImpulseReceiver movingReceiver)
    {
        float distance = movement.magnitude;
        if (distance <= 0.0001f) return false;

        int count = Physics.SphereCastNonAlloc(origin, collisionRadius, movement / distance,
            orbitHits, distance, hitMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider hitCollider = orbitHits[i].collider;
            ImpulseReceiver other = hitCollider.GetComponentInParent<ImpulseReceiver>();
            if (hitCollider.transform.root == transform.root || other == movingReceiver) continue;

            if (other == null || !other.CanBeExternallyMoved) return true;
            ApplyCollisionImpulse(other, movement);
        }

        return false;
    }

    private void ApplyCollisionImpulse(ImpulseReceiver receiver, Vector3 movement)
    {
        if (receiver == lastImpulsedReceiver && Time.time < nextCollisionImpulseTime) return;

        Vector2 direction = new Vector2(movement.x, movement.z).normalized;
        receiver.ApplyImpulse(direction, collisionImpulseForce, gameObject);
        lastImpulsedReceiver = receiver;
        nextCollisionImpulseTime = Time.time + 0.15f;
    }

    private void SwitchToPlayerOrbit()
    {
        anchorTransform = targetReceiver.transform;
        anchorLocalPoint = anchorTransform.InverseTransformPoint(targetBody.position);
        orbitOffset = rb.position - targetBody.position;
        orbitOffset.y = 0f;
        controller.enableMovement = false;
        state = GrabState.PlayerOrbitingAnchor;
    }

    public void CancelGrab(bool applyInertia)
    {
        GrabState previousState = state;
        state = GrabState.Idle;

        if (projectile != null) Destroy(projectile.gameObject);
        projectile = null;

        if (targetReceiver != null)
        {
            targetReceiver.EndExternalControl();
            if (applyInertia && previousState == GrabState.TargetOrbitingPlayer)
                targetReceiver.ApplyImpulse(ToVector2(lastMotionDirection), releaseImpulseForce, gameObject);
        }

        if (controller != null)
        {
            controller.enableMovement = true;
            if (applyInertia && previousState == GrabState.PlayerOrbitingAnchor)
                controller.AddImpulse(lastMotionDirection * releaseImpulseForce);
        }

        targetReceiver = null;
        targetBody = null;
        anchorTransform = null;
        lastMotionDirection = Vector3.zero;
        lastImpulsedReceiver = null;
    }

    private Vector3 RotateClockwise(Vector3 offset)
    {
        return Quaternion.AngleAxis(rotationSpeed * Time.fixedDeltaTime, Vector3.down) * offset;
    }

    private void RememberMotion(Vector3 movement)
    {
        if (movement.sqrMagnitude > 0.000001f)
            lastMotionDirection = movement.normalized;
    }

    private Vector3 GetAnchorPosition()
    {
        return anchorTransform != null ? anchorTransform.TransformPoint(anchorLocalPoint) : transform.position;
    }

    private Vector3 GetAimDirection()
    {
        if (mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, transform.position);
            if (plane.Raycast(ray, out float distance))
            {
                Vector3 direction = ray.GetPoint(distance) - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f) return direction.normalized;
            }
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    private static Vector2 ToVector2(Vector3 direction)
    {
        return new Vector2(direction.x, direction.z);
    }

    private void OnProjectileMissed()
    {
        projectile = null;
        CancelGrab(false);
    }

    private void OnAbilityActivated(ActiveAbility ability)
    {
        if (ability != this && state != GrabState.Idle) CancelGrab(true);
    }

    private void OnPlayerDamaged(int damage) => CancelGrab(true);
    private void OnPlayerDied() => CancelGrab(false);

    private void OnDisable()
    {
        UnsubscribeFromInterruptions();

        CancelGrab(false);
    }

    private void SubscribeToInterruptions()
    {
        if (abilityManager == null) return;

        abilityManager.AbilityActivated -= OnAbilityActivated;
        abilityManager.AbilityActivated += OnAbilityActivated;
        if (health != null)
        {
            health.onDamaged.RemoveListener(OnPlayerDamaged);
            health.onDeath.RemoveListener(OnPlayerDied);
            health.onDamaged.AddListener(OnPlayerDamaged);
            health.onDeath.AddListener(OnPlayerDied);
        }

        if (knockoutSystem == null) return;
        knockoutSystem.onKnockoutStarted.RemoveListener(OnPlayerKnockedOut);
        knockoutSystem.onGameOver.RemoveListener(OnPlayerDied);
        knockoutSystem.onKnockoutStarted.AddListener(OnPlayerKnockedOut);
        knockoutSystem.onGameOver.AddListener(OnPlayerDied);
    }

    private void UnsubscribeFromInterruptions()
    {
        if (abilityManager != null) abilityManager.AbilityActivated -= OnAbilityActivated;
        if (health != null)
        {
            health.onDamaged.RemoveListener(OnPlayerDamaged);
            health.onDeath.RemoveListener(OnPlayerDied);
        }

        if (knockoutSystem == null) return;
        knockoutSystem.onKnockoutStarted.RemoveListener(OnPlayerKnockedOut);
        knockoutSystem.onGameOver.RemoveListener(OnPlayerDied);
    }

    private void OnPlayerKnockedOut() => CancelGrab(true);
}
