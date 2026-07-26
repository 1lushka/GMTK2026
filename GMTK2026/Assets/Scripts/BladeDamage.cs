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
        Rigidbody targetBody = other.attachedRigidbody;
        bool isPlayer = other.CompareTag("Player") ||
                        (targetBody != null && targetBody.CompareTag("Player"));
        if (isPlayer)
        {
            
            KnockoutAPI.TakeDamage(damage);
            

            
        }

        if (allowHealthFallback)
        {
            HealthComponent targetHealth = other.GetComponentInParent<HealthComponent>();
            if (targetHealth != null)
                targetHealth.TakeDamage(damage);
        }
    }
}
