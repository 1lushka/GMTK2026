using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public sealed class ImpulseReceiver : MonoBehaviour, IReflectable
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
    private Vector2 decelerationStartVelocity;
    private float decelerationElapsed;
    private bool externallyControlled;
    private readonly HashSet<ImpulseReceiver> collidedReceivers = new HashSet<ImpulseReceiver>();

    public event Action<ImpulseInfo> ImpulseReceived;

    public State CurrentState => currentState;
    public Vector2 CurrentVelocity => currentVelocity;
    public float CurrentSpeed => currentSpeed;
    public float RemainingFlightTime => remainingFlightTime;
    public float RemainingLockTime => remainingLockTime;
    public bool IsMoving => currentState == State.Flying || currentState == State.Decelerating;
    public bool IsTemporaryLocked => currentState == State.TemporaryLocked;
    public bool IsMovable => profile != null && profile.IsMovable;
    public bool CanBeExternallyMoved => enabled && IsMovable && !IsTemporaryLocked;
    public GameObject ReflectionObject => gameObject;
    public bool CanBeReflected => enabled && IsMoving && currentVelocity.sqrMagnitude > 0f;
    public Vector3 MovementVelocity => new Vector3(currentVelocity.x, 0f, currentVelocity.y);

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
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
        direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.zero;
        force = Mathf.Max(0f, force);

        var info = new ImpulseInfo(direction, force, source);
        ImpulseReceived?.Invoke(info);
        onImpulseReceived?.Invoke(direction, force, source);

        if (!enabled || !profile.IsMovable || IsTemporaryLocked || direction == Vector2.zero || force <= 0f)
            return;

        Vector2 addedVelocity = direction * (force * profile.SpeedPerForce);
        currentVelocity = Vector2.ClampMagnitude(currentVelocity + addedVelocity, profile.MaxSpeed);
        currentSpeed = currentVelocity.magnitude;
        remainingFlightTime = profile.FlightTime;
        currentState = State.Flying;
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

    public void Reflect(Vector3 surfaceNormal)
    {
        if (!CanBeReflected) return;

        Vector3 reflected = Vector3.Reflect(MovementVelocity, surfaceNormal);
        currentVelocity = new Vector2(reflected.x, reflected.z);
        decelerationStartVelocity = currentState == State.Decelerating
            ? currentVelocity.normalized * decelerationStartVelocity.magnitude
            : decelerationStartVelocity;
        currentSpeed = currentVelocity.magnitude;
    }

    public void SeparateFromSurface(Vector3 offset)
    {
        body.MovePosition(body.position + offset);
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

        currentSpeed = currentVelocity.magnitude;
    }

    private void Move(float deltaTime)
    {
        Vector3 displacement = new Vector3(currentVelocity.x, 0f, currentVelocity.y);
        body.MovePosition(body.position + displacement * deltaTime);
        body.linearVelocity = Vector3.zero;
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

        MagicScreenReflector localReflector = GetComponent<MagicScreenReflector>();
        IReflectable otherReflectable = FindReflectable(collision.collider);
        if (localReflector != null && localReflector.TryHandleCollision(collision, otherReflectable)) return;

        MagicScreenReflector otherReflector = collision.collider.GetComponentInParent<MagicScreenReflector>();
        if (otherReflector != null && otherReflector.TryHandleCollision(collision, this)) return;

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

    private static IReflectable FindReflectable(Collider targetCollider)
    {
        MonoBehaviour[] behaviours = targetCollider.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IReflectable reflectable) return reflectable;
        }

        return null;
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
