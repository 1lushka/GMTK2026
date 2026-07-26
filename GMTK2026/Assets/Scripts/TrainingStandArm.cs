using UnityEngine;

public class TrainingStandArm : MonoBehaviour
{
    private TrainingStand stand;

    private void Awake()
    {
        stand = GetComponentInParent<TrainingStand>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (stand != null)
            stand.OnArmCollision(collision.collider, collision.GetContact(0).point, collision.relativeVelocity);
    }
}
