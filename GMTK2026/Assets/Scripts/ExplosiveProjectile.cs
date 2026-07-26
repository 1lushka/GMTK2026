using System.Collections.Generic;
using UnityEngine;

public class ExplosiveProjectile : MonoBehaviour, IReflectable
{
    private float speed;
    private float explosionForce;
    private float explosionRadius;
    private GameObject explosionEffectPrefab;
    private bool exploded;
    private Rigidbody body;

    public GameObject ReflectionObject => gameObject;
    public bool CanBeReflected => !exploded && body != null && body.linearVelocity.sqrMagnitude > 0f;
    public Vector3 MovementVelocity => body != null ? body.linearVelocity : Vector3.zero;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void Initialize(float speed, float force, float radius, GameObject effectPrefab)
    {
        this.speed = speed;
        this.explosionForce = force;
        this.explosionRadius = radius;
        this.explosionEffectPrefab = effectPrefab;

        if (body != null)
            body.linearVelocity = transform.forward * speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (exploded) return;
        MagicScreenReflector reflector = collision.collider.GetComponentInParent<MagicScreenReflector>();
        if (reflector != null && reflector.TryHandleCollision(collision, this)) return;
        Explode();
    }

    public void Reflect(Vector3 surfaceNormal)
    {
        if (!CanBeReflected) return;
        body.linearVelocity = Vector3.Reflect(body.linearVelocity, surfaceNormal);
        if (body.linearVelocity.sqrMagnitude > 0f)
            transform.rotation = Quaternion.LookRotation(body.linearVelocity.normalized, Vector3.up);
    }

    public void SeparateFromSurface(Vector3 offset)
    {
        body.position += offset;
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
