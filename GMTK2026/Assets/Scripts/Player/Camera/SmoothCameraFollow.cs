using UnityEngine;

public class SimpleSmoothCamera : MonoBehaviour
{
    [SerializeField] private Transform target;                
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -6); 
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private float maxSpeed = 50f;

    private Vector3 currentVelocity;

    void FixedUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            smoothTime,
            maxSpeed
        );
    }
}