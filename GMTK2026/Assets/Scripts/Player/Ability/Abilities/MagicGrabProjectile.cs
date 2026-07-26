using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class MagicGrabProjectile : MonoBehaviour
{
    private readonly RaycastHit[] hits = new RaycastHit[8];

    private Action<RaycastHit> onHit;
    private Action onMiss;
    private GameObject owner;
    private Vector3 direction;
    private Vector3 startPosition;
    private float speed;
    private float maxDistance;
    private float castRadius;
    private LayerMask hitMask;
    private bool initialized;

    public void Initialize(GameObject projectileOwner, Vector3 flyDirection, float projectileSpeed,
        float distance, float radius, LayerMask mask, Action<RaycastHit> hit, Action miss)
    {
        owner = projectileOwner;
        direction = flyDirection.normalized;
        speed = projectileSpeed;
        maxDistance = distance;
        castRadius = radius;
        hitMask = mask;
        onHit = hit;
        onMiss = miss;
        startPosition = transform.position;
        initialized = true;
    }

    private void FixedUpdate()
    {
        if (!initialized) return;

        float step = speed * Time.fixedDeltaTime;
        if (TryFindHit(step, out RaycastHit hit))
        {
            onHit?.Invoke(hit);
            Destroy(gameObject);
            return;
        }

        transform.position += direction * step;
        if ((transform.position - startPosition).sqrMagnitude >= maxDistance * maxDistance)
        {
            onMiss?.Invoke();
            Destroy(gameObject);
        }
    }

    private bool TryFindHit(float distance, out RaycastHit closestHit)
    {
        int count = Physics.SphereCastNonAlloc(transform.position, castRadius, direction, hits,
            distance, hitMask, QueryTriggerInteraction.Ignore);
        float closestDistance = float.MaxValue;
        closestHit = default;

        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = hits[i];
            if (candidate.collider.transform.root.gameObject == owner || candidate.distance >= closestDistance)
                continue;

            closestDistance = candidate.distance;
            closestHit = candidate;
        }

        return closestDistance < float.MaxValue;
    }

    private void OnDestroy()
    {
        onHit = null;
        onMiss = null;
    }
}
