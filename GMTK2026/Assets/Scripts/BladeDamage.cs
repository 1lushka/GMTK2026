using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class BladeDamage : MonoBehaviour
{
    [SerializeField, Min(0)] private int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        HealthComponent targetHealth = other.GetComponentInParent<HealthComponent>();
        if (targetHealth != null)
            targetHealth.TakeDamage(damage);
    }
}
