using UnityEngine;

public class FallTrap : MonoBehaviour
{
 

    private void OnTriggerEnter(Collider other)
    {
        HealthComponent targetHealth = other.GetComponentInParent<HealthComponent>();
        if (targetHealth != null)
        {

            targetHealth.TakeDamage(100);
            Debug.Log("FallTrap: " + other.name + " took damage from trap.");
        }

    }
}
