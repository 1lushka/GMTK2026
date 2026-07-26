using ForgettingBoxer.Knockout;
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
        {
            if (!targetHealth.CompareTag("Player") || !KnockoutAPI.TakeDamage(damage))
                targetHealth.TakeDamage(damage);
        }
    }
}
