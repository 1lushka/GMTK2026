using UnityEngine;
using System.Collections;

public class DummyEnemy : MonoBehaviour
{
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float destroyDelay = 0.5f;

    private HealthComponent health;
    private Collider mainCollider;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        mainCollider = GetComponent<Collider>();

        if (health != null)
        {
            health.onDeath.AddListener(HandleDeath);
        }
    }

    private void HandleDeath()
    {
        if (mainCollider != null) mainCollider.enabled = false;
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    public void TakeDamage(int damage)
    {
        health?.TakeDamage(damage);
    }
}