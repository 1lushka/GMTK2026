using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(ImpulseReceiver))]
[DisallowMultipleComponent]
public sealed class MagicScreenReflector : MonoBehaviour
{
    [SerializeField, Min(0f)] private float sameReflectorCooldown = 0.08f;
    [SerializeField] private LayerMask reflectionCollisionMask = ~0;
    [SerializeField, Min(0f)] private float minimumIncomingSpeed = 0.1f;
    [SerializeField, Min(0f)] private float separationOffset = 0.02f;
    [SerializeField] private bool debugReflection;

    private readonly Dictionary<int, float> reflectionCooldowns = new Dictionary<int, float>();
    private Collider surfaceCollider;
    private Vector3 lastIncomingVelocity;
    private Vector3 lastSurfaceNormal;
    private Vector3 lastReflectedVelocity;

    private void Awake()
    {
        surfaceCollider = GetComponent<Collider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHandleCollision(collision, FindReflectable(collision.collider));
    }

    public bool TryHandleCollision(Collision collision, IReflectable reflectable)
    {
        if (reflectable == null || reflectable.ReflectionObject == gameObject)
            return false;

        GameObject target = reflectable.ReflectionObject;
        if ((reflectionCollisionMask.value & (1 << target.layer)) == 0)
            return false;

        int targetId = target.GetInstanceID();
        if (reflectionCooldowns.TryGetValue(targetId, out float cooldownEnd) && Time.time < cooldownEnd)
            return true;

        Vector3 incomingVelocity = reflectable.MovementVelocity;
        if (!reflectable.CanBeReflected || incomingVelocity.magnitude < minimumIncomingSpeed)
            return false;

        Vector3 normal = GetSurfaceNormal(collision, target.transform.position);
        if (Vector3.Dot(incomingVelocity, normal) >= -minimumIncomingSpeed)
            return false;

        reflectable.Reflect(normal);
        Vector3 reflectedVelocity = reflectable.MovementVelocity;
        if (reflectedVelocity.sqrMagnitude <= 0f)
            return false;

        reflectionCooldowns[targetId] = Time.time + sameReflectorCooldown;
        reflectable.SeparateFromSurface(normal * separationOffset);
        RememberDebug(incomingVelocity, normal, reflectedVelocity);
        return true;
    }

    private Vector3 GetSurfaceNormal(Collision collision, Vector3 targetPosition)
    {
        Vector3 surfacePoint = surfaceCollider.ClosestPoint(targetPosition);
        Vector3 normal = targetPosition - surfacePoint;
        if (normal.sqrMagnitude > 0.000001f) return normal.normalized;

        normal = collision.contactCount > 0 ? collision.GetContact(0).normal : transform.forward;
        Vector3 centreToTarget = targetPosition - surfaceCollider.bounds.center;
        if (Vector3.Dot(normal, centreToTarget) < 0f) normal = -normal;
        return normal.normalized;
    }

    private static IReflectable FindReflectable(Collider other)
    {
        MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IReflectable reflectable) return reflectable;
        }

        return null;
    }

    private void RememberDebug(Vector3 incoming, Vector3 normal, Vector3 reflected)
    {
        if (!debugReflection) return;
        lastIncomingVelocity = incoming;
        lastSurfaceNormal = normal;
        lastReflectedVelocity = reflected;
    }

    private void OnDisable()
    {
        reflectionCooldowns.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugReflection) return;
        Vector3 origin = surfaceCollider != null ? surfaceCollider.bounds.center : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, lastIncomingVelocity);
        Gizmos.color = Color.white;
        Gizmos.DrawRay(origin, lastSurfaceNormal);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, lastReflectedVelocity);
    }
}
