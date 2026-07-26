using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemySightTrigger : MonoBehaviour
{
    private EnemyController enemyController;

    private void Awake()
    {
        enemyController = GetComponentInParent<EnemyController>();
        if (enemyController == null)
        {
            Debug.LogError("EnemySightTrigger must be a child of an object with EnemyController!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && enemyController != null)
        {
            enemyController.Alert();
        }
    }
}