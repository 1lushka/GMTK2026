using System.Collections.Generic;
using UnityEngine;

public class ExplosiveProjectile : MonoBehaviour
{
    private float speed;
    private float explosionForce;
    private float explosionRadius;
    private GameObject explosionEffectPrefab;
    private bool exploded;

    public void Initialize(float speed, float force, float radius, GameObject effectPrefab)
    {
        this.speed = speed;
        this.explosionForce = force;
        this.explosionRadius = radius;
        this.explosionEffectPrefab = effectPrefab;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = transform.forward * speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (exploded) return;
        Explode();
    }

    private void Explode()
    {
        exploded = true;

        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        var affectedReceivers = new HashSet<ImpulseReceiver>();
        foreach (Collider hit in colliders)
        {
            ImpulseReceiver receiver = hit.GetComponentInParent<ImpulseReceiver>();
            if (receiver == null || !affectedReceivers.Add(receiver)) continue;

            Vector3 offset = receiver.transform.position - transform.position;
            Vector2 direction = new Vector2(offset.x, offset.z);
            if (direction.sqrMagnitude <= 0.0001f) direction = Random.insideUnitCircle;
            receiver.ApplyImpulse(direction, explosionForce, gameObject);
        }

        Destroy(gameObject);
    }
}
