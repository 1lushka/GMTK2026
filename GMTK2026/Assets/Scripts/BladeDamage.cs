using ForgettingBoxer.Knockout;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class BladeDamage : MonoBehaviour
{
    [SerializeField, Min(0)] private int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        ApplyDamage(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        ApplyDamage(other, false);
    }

    private void ApplyDamage(Collider other, bool allowHealthFallback)
    {
        HealthComponent targetHealth = other.GetComponentInParent<HealthComponent>();
        if (targetHealth == null) return;

        if (targetHealth.CompareTag("Player"))
        {
            if (!KnockoutAPI.TakeDamage(damage) && allowHealthFallback)
                targetHealth.TakeDamage(damage);
            return;
        }

        if (allowHealthFallback)
            targetHealth.TakeDamage(damage);
    }
}
