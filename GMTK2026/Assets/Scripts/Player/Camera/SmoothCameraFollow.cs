using UnityEngine;

public class SimpleSmoothCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -6);
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private float maxSpeed = 50f;

    [Header("Limits")]
    [Tooltip("Collider containing every allowed camera position. Keep it enabled; use Is Trigger if it should not affect physics.")]
    [SerializeField] private Collider cameraVolume;
    [Min(0f)]
    [Tooltip("Camera-to-target distance that triggers an instant snap. Set to 0 to disable snapping.")]
    [SerializeField] private float criticalDistance = 30f;

    private Vector3 currentVelocity;

    private void FixedUpdate()
    {
        if (target == null)
        {
            transform.position = ConstrainToVolume(transform.position);
            return;
        }

        Vector3 defaultPosition = target.position + offset;

        if (ShouldSnapToTarget())
        {
            currentVelocity = Vector3.zero;
            transform.position = ConstrainToVolume(defaultPosition);
            return;
        }

        Vector3 nextPosition = Vector3.SmoothDamp(
            transform.position,
            ConstrainToVolume(defaultPosition),
            ref currentVelocity,
            smoothTime,
            maxSpeed
        );

        transform.position = ConstrainToVolume(nextPosition);
    }

    private bool ShouldSnapToTarget()
    {
        if (criticalDistance <= 0f)
        {
            return false;
        }

        float criticalDistanceSquared = criticalDistance * criticalDistance;
        return (transform.position - target.position).sqrMagnitude > criticalDistanceSquared;
    }

    private Vector3 ConstrainToVolume(Vector3 position)
    {
        return cameraVolume == null ? position : cameraVolume.ClosestPoint(position);
    }
}
