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
    [SerializeField, Min(0f)] private float orbitLinearSpeed = 6f;
    [SerializeField] private float collisionImpulseForce = 8f;
    [SerializeField] private float releaseImpulseForce = 10f;
    [SerializeField] private float playerCollisionRadius = 0.45f;
    [SerializeField] private float groundCheckDistance = 2f;

    [Header("Optional Visual Hook")]
    [SerializeField] private Transform linkOrigin;

    private readonly RaycastHit[] orbitHits = new RaycastHit[12];
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

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
    private float orbitDirection = -1f;
    private float playerOrbitHeight;
    private HealthComponent health;
    private KnockoutSystem knockoutSystem;
    private Camera mainCamera;
    private Collider playerCollider;

    public bool IsActive => state != GrabState.Idle;
    public Transform LinkOrigin => linkOrigin != null ? linkOrigin : transform;
    public Vector3 LinkTarget => GetAnchorPosition();

    protected override void Start()
    {
        base.Start();
        mainCamera = Camera.main;
        health = GetComponent<HealthComponent>();
        playerCollider = GetComponentInChildren<Collider>();
        CachePlayerCollisionRadius();
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
        controller.ActivateRotation(false);
        state = GrabState.TargetOrbitingPlayer;
    }

    private void BeginPlayerOrbit(Transform anchor, Vector3 worldPoint)
    {
        anchorTransform = anchor;
        anchorLocalPoint = anchor.InverseTransformPoint(worldPoint);
        orbitOffset = rb.position - worldPoint;
        orbitOffset.y = 0f;
        playerOrbitHeight = rb.position.y;
        controller.enableMovement = false;
        controller.ActivateRotation(false);
        controller.SetVelocity(Vector3.zero);
        rb.linearVelocity = Vector3.zero;
        state = GrabState.PlayerOrbitingAnchor;
    }

    private void OrbitTargetAroundPlayer()
    {
        if (targetBody == null)
        {
            CancelGrab(false);
            return;
        }

        Vector3 desiredOffset = RotateOrbit(orbitOffset);
        Vector3 movement = rb.position + desiredOffset - targetBody.position;
        if (HandleOrbitCollisions(targetBody.position, movement, targetReceiver))
        {
            orbitDirection *= -1f;
            RememberMotion(-movement);
            FaceAnchor(targetBody.position);
            return;
        }

        targetBody.MovePosition(targetBody.position + movement);
        orbitOffset = desiredOffset;
        RememberMotion(movement);
        FaceAnchor(targetBody.position + movement);
    }

    private void OrbitPlayerAroundAnchor()
    {
        Vector3 anchorPosition = GetAnchorPosition();
        rb.linearVelocity = Vector3.zero;
        Vector3 desiredOffset = RotateOrbit(orbitOffset);
        Vector3 desiredPosition = anchorPosition + desiredOffset;
        desiredPosition.y = playerOrbitHeight;
        Vector3 movement = desiredPosition - rb.position;

        if (!CanMovePlayer(movement, desiredPosition))
        {
            orbitDirection *= -1f;
            RememberMotion(-movement);
            FaceAnchor(anchorPosition);
            return;
        }

        rb.MovePosition(rb.position + movement);
        orbitOffset = desiredOffset;
        RememberMotion(movement);
        FaceAnchor(anchorPosition);
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
            controller.ActivateRotation(true);
            if (applyInertia && previousState == GrabState.PlayerOrbitingAnchor)
                controller.AddImpulse(lastMotionDirection * releaseImpulseForce);
        }

        targetReceiver = null;
        targetBody = null;
        anchorTransform = null;
        lastMotionDirection = Vector3.zero;
        lastImpulsedReceiver = null;
    }

    private Vector3 RotateOrbit(Vector3 offset)
    {
        float radius = offset.magnitude;
        if (radius <= 0.0001f) return offset;

        float angle = orbitLinearSpeed / radius * Mathf.Rad2Deg * orbitDirection * Time.fixedDeltaTime;
        return Quaternion.AngleAxis(angle, Vector3.up) * offset;
    }

    private bool CanMovePlayer(Vector3 movement, Vector3 desiredPosition)
    {
        float distance = movement.magnitude;
        if (distance <= 0.0001f) return true;

        int count = Physics.SphereCastNonAlloc(rb.position, playerCollisionRadius,
            movement / distance, orbitHits, distance, hitMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (orbitHits[i].collider.transform.root != transform.root)
                return false;
        }

        return HasGroundAt(desiredPosition);
    }

    private void CachePlayerCollisionRadius()
    {
        if (playerCollider == null) return;

        Vector3 extents = playerCollider.bounds.extents;
        playerCollisionRadius = Mathf.Max(playerCollisionRadius, extents.x, extents.z);
    }

    private bool HasGroundAt(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * 0.25f;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits,
            groundCheckDistance, hitMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (groundHits[i].collider.transform.root != transform.root)
                return true;
        }

        return false;
    }

    private void FaceAnchor(Vector3 anchorPosition)
    {
        Vector3 direction = anchorPosition - rb.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            rb.MoveRotation(Quaternion.LookRotation(direction));
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
