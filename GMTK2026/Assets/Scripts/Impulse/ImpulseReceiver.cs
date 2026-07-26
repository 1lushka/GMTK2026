using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public sealed class ImpulseReceiver : MonoBehaviour
{
    public enum State
    {
        Idle,
        Flying,
        Decelerating,
        TemporaryLocked
    }

    [Serializable]
    private sealed class ImpulseUnityEvent : UnityEvent<Vector2, float, GameObject> { }

    [SerializeField] private ImpulseProfile profile;
    [SerializeField] private ImpulseUnityEvent onImpulseReceived;

    [Header("Runtime Debug")]
    [SerializeField] private State currentState;
    [SerializeField] private Vector2 currentVelocity;
    [SerializeField] private float currentSpeed;
    [SerializeField] private float remainingFlightTime;
    [SerializeField] private float remainingLockTime;
    [SerializeField] private bool drawVelocityGizmo = true;

    private Rigidbody body;
    private DashAbility dashAbility;
    private Vector2 decelerationStartVelocity;
    private float decelerationElapsed;
    private bool externallyControlled;
    private bool airborne;
    private bool originalUseGravity;
    private float groundY;
    private readonly HashSet<ImpulseReceiver> collidedReceivers = new HashSet<ImpulseReceiver>();

    public event Action<ImpulseInfo> ImpulseReceived;
    public event Action<ImpulseInfo> ImpulseApplied;

    public State CurrentState => currentState;
    public Vector2 CurrentVelocity => currentVelocity;
    public float CurrentSpeed => currentSpeed;
    public float RemainingFlightTime => remainingFlightTime;
    public float RemainingLockTime => remainingLockTime;
    public GameObject LastImpulseSource { get; private set; }
    public bool IsMoving => currentState == State.Flying || currentState == State.Decelerating;
    public bool IsTemporaryLocked => currentState == State.TemporaryLocked;
    public bool IsAirborne => airborne;
    public bool IsMovable => profile != null && profile.IsMovable;
    public bool CanBeExternallyMoved => enabled && IsMovable && !IsTemporaryLocked;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        dashAbility = GetComponentInParent<DashAbility>();
        body.linearVelocity = Vector3.zero;
        currentState = State.Idle;

        if (profile == null)
        {
            Debug.LogError("ImpulseReceiver requires an ImpulseProfile.", this);
            enabled = false;
            return;
        }

        if (profile.IsMovable && body.isKinematic)
        {
            Debug.LogError("A movable ImpulseReceiver requires a non-kinematic Rigidbody.", this);
            enabled = false;
        }
    }

    public void ApplyImpulse(Vector2 direction, float force, GameObject source = null)
    {
        ApplyImpulseInternal(direction, force, source, false);
    }

    public void ForceApplyImpulse(Vector2 direction, float force, GameObject source = null)
    {
        ApplyImpulseInternal(direction, force, source, true);
    }

    private void ApplyImpulseInternal(Vector2 direction, float force, GameObject source, bool ignoreLock)
    {
        if (dashAbility != null && dashAbility.IsDashing) return;

        direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.zero;
        force = Mathf.Max(0f, force);

        var info = new ImpulseInfo(direction, force, source);
        ImpulseReceived?.Invoke(info);
        onImpulseReceived?.Invoke(direction, force, source);

        if (!enabled || !profile.IsMovable || (!ignoreLock && IsTemporaryLocked) ||
            direction == Vector2.zero || force <= 0f)
            return;

        LastImpulseSource = source;

        if (!airborne)
        {
            groundY = body.position.y;
            originalUseGravity = body.useGravity;
            body.useGravity = false;
            airborne = true;
        }

        Vector2 addedVelocity = direction * (force * profile.SpeedPerForce);
        currentVelocity = Vector2.ClampMagnitude(currentVelocity + addedVelocity, profile.MaxSpeed);
        currentSpeed = currentVelocity.magnitude;
        remainingFlightTime = profile.FlightTime;
        currentState = State.Flying;
        ImpulseApplied?.Invoke(info);
    }

    public bool BeginExternalControl()
    {
        if (!CanBeExternallyMoved) return false;

        externallyControlled = true;
        currentVelocity = Vector2.zero;
        currentSpeed = 0f;
        remainingFlightTime = 0f;
        currentState = State.Idle;
        collidedReceivers.Clear();
        body.linearVelocity = Vector3.zero;
        return true;
    }

    public void EndExternalControl()
    {
        externallyControlled = false;
    }

    private void FixedUpdate()
    {
        if (externallyControlled) return;

        float deltaTime = Time.fixedDeltaTime;

        switch (currentState)
        {
            case State.Flying:
                Move(deltaTime);
                remainingFlightTime -= deltaTime;
                if (remainingFlightTime <= 0f) BeginDeceleration();
                break;

            case State.Decelerating:
                UpdateDeceleration(deltaTime);
                Move(deltaTime);
                break;

            case State.TemporaryLocked:
                remainingLockTime -= deltaTime;
                if (remainingLockTime <= 0f) SetIdle();
                break;
        }

        if (airborne && !IsMoving)
            Move(deltaTime);

        currentSpeed = currentVelocity.magnitude;
    }

    private void Move(float deltaTime)
    {
        Vector3 displacement = new Vector3(currentVelocity.x, 0f, currentVelocity.y);
        Vector3 nextPosition = body.position + displacement * deltaTime;
        float targetY = currentState == State.Flying ? groundY + profile.FlightHeight : groundY;
        float duration = currentState == State.Flying ? profile.LiftDuration : profile.LandingDuration;
        nextPosition.y = Mathf.MoveTowards(body.position.y, targetY,
            profile.FlightHeight / Mathf.Max(0.01f, duration) * deltaTime);
        body.MovePosition(nextPosition);
        body.linearVelocity = Vector3.zero;

        if (currentState != State.Flying && Mathf.Approximately(nextPosition.y, groundY))
        {
            airborne = false;
            body.useGravity = originalUseGravity;
        }
    }

    private void BeginDeceleration()
    {
        currentState = State.Decelerating;
        remainingFlightTime = 0f;
        decelerationStartVelocity = currentVelocity;
        decelerationElapsed = 0f;
    }

    private void UpdateDeceleration(float deltaTime)
    {
        decelerationElapsed += deltaTime;
        float progress = Mathf.Clamp01(decelerationElapsed / profile.DecelerationTime);
        currentVelocity = Vector2.Lerp(decelerationStartVelocity, Vector2.zero, progress);
        if (progress >= 1f) StopAndLock();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsMoving) return;
        if (IsLandingCollision(collision)) return;
        if (collision.collider.GetComponentInParent<TrainingStandArm>() != null) return;

        ImpulseReceiver other = collision.collider.GetComponentInParent<ImpulseReceiver>();
        if (other == null)
        {
            StopAndLock();
            return;
        }

        if (other.IsTemporaryLocked || !other.IsMovable)
        {
            StopAndLock();
            return;
        }

        if (collidedReceivers.Contains(other)) return;
        TransferImpulse(other);
    }

    private static bool IsLandingCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
                return true;
        }

        return false;
    }

    private void TransferImpulse(ImpulseReceiver other)
    {
        collidedReceivers.Add(other);
        other.collidedReceivers.Add(this);

        Vector3 offset = other.transform.position - transform.position;
        Vector2 direction = new Vector2(offset.x, offset.z);
        if (direction.sqrMagnitude <= 0.0001f) direction = currentVelocity;
        direction.Normalize();

        float transferForce = currentSpeed * profile.ImpulseTransferMultiplier;
        other.ApplyImpulse(direction, transferForce, gameObject);

        currentVelocity *= profile.CollisionSpeedMultiplier;
        decelerationStartVelocity *= profile.CollisionSpeedMultiplier;
        currentSpeed = currentVelocity.magnitude;
    }

    private void StopAndLock()
    {
        currentVelocity = Vector2.zero;
        currentSpeed = 0f;
        remainingFlightTime = 0f;
        remainingLockTime = profile.StopLockDuration;
        currentState = remainingLockTime > 0f ? State.TemporaryLocked : State.Idle;
        body.linearVelocity = Vector3.zero;
    }

    private void SetIdle()
    {
        remainingLockTime = 0f;
        currentState = State.Idle;
        LastImpulseSource = null;
        collidedReceivers.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawVelocityGizmo || currentVelocity.sqrMagnitude <= 0f) return;
        Gizmos.color = Color.cyan;
        Vector3 direction = new Vector3(currentVelocity.x, 0f, currentVelocity.y);
        Gizmos.DrawRay(transform.position, direction);
    }
}
