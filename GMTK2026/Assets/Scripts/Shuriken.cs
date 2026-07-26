using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(HealthComponent))]
public class Shuriken : MonoBehaviour
{
    private GameObject owner;
    private bool launched;

    public void Initialize(Vector3 velocity, GameObject owner)
    {
        this.owner = owner;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = velocity;
        launched = true;

        Collider ownerCollider = owner.GetComponent<Collider>();
        if (ownerCollider != null)
        {
            Collider myCollider = GetComponent<Collider>();
            Physics.IgnoreCollision(myCollider, ownerCollider, true);
        }

        HealthComponent health = GetComponent<HealthComponent>();
        if (health != null)
            health.onDamaged.AddListener(OnDamaged);

        Destroy(gameObject, 5f);
    }

    private void OnDamaged(int damage)
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!launched) return;

        if (collision.gameObject != owner)
        {
            HealthComponent targetHealth = collision.gameObject.GetComponentInParent<HealthComponent>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(1);
            }
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        HealthComponent health = GetComponent<HealthComponent>();
        if (health != null)
            health.onDamaged.RemoveListener(OnDamaged);
    }
}