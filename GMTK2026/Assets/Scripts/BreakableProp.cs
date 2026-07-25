using UnityEngine;

[RequireComponent(typeof(HealthComponent), typeof(Rigidbody))]
public class BreakableProp : MonoBehaviour
{
    [Header("Collision Damage")]
    [SerializeField] private float minCollisionSpeed = 4f;      
    [SerializeField] private int collisionDamage = 1;           
    [SerializeField] private bool destroyOnDeath = true;        
    [SerializeField] private float destroyDelay = 0.5f;         

    private HealthComponent health;
    private Rigidbody rb;
    private bool dead;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        rb = GetComponent<Rigidbody>();

        if (health != null)
        {
            health.onDamaged.AddListener(OnDamaged);
            health.onDeath.AddListener(OnDeath);
        }
    }

    private void OnDamaged(int damage)
    {
        
    }

    private void OnDeath()
    {
        dead = true;
        if (destroyOnDeath)
            StartCoroutine(DestroyAfterDelay());
        
    }

    private System.Collections.IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (dead) return; 

        float relativeSpeed = collision.relativeVelocity.magnitude;

        if (relativeSpeed >= minCollisionSpeed)
        {
            
            health?.TakeDamage(collisionDamage);

            
            HealthComponent otherHealth = collision.collider.GetComponentInParent<HealthComponent>();
            if (otherHealth != null)
            {
                otherHealth.TakeDamage(collisionDamage);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        health?.TakeDamage(damage);
    }
}