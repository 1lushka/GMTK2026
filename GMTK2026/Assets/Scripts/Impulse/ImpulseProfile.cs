using UnityEngine;

[CreateAssetMenu(fileName = "ImpulseProfile", menuName = "GMTK/Impulse Profile")]
public sealed class ImpulseProfile : ScriptableObject
{
    [SerializeField] private bool isMovable = true;
    [SerializeField, Min(0f)] private float speedPerForce = 1f;
    [SerializeField, Min(0f)] private float flightTime = 0.35f;
    [SerializeField, Min(0.01f)] private float decelerationTime = 0.1f;
    [SerializeField, Min(0f)] private float stopLockDuration = 0.15f;
    [SerializeField, Min(0f)] private float maxSpeed = 20f;
    [SerializeField, Min(0f)] private float impulseTransferMultiplier = 0.75f;
    [SerializeField, Range(0f, 1f)] private float collisionSpeedMultiplier = 0.8f;
    [Header("Flight Height")]
    [SerializeField, Min(0f)] private float flightHeight = 1.25f;
    [SerializeField, Min(0.01f)] private float liftDuration = 0.08f;
    [SerializeField, Min(0.01f)] private float landingDuration = 0.12f;

    public bool IsMovable => isMovable;
    public float SpeedPerForce => speedPerForce;
    public float FlightTime => flightTime;
    public float DecelerationTime => decelerationTime;
    public float StopLockDuration => stopLockDuration;
    public float MaxSpeed => maxSpeed;
    public float ImpulseTransferMultiplier => impulseTransferMultiplier;
    public float CollisionSpeedMultiplier => collisionSpeedMultiplier;
    public float FlightHeight => flightHeight;
    public float LiftDuration => liftDuration;
    public float LandingDuration => landingDuration;
}
