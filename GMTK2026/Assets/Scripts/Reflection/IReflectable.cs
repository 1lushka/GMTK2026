using UnityEngine;

public interface IReflectable
{
    GameObject ReflectionObject { get; }
    bool CanBeReflected { get; }
    Vector3 MovementVelocity { get; }
    void Reflect(Vector3 surfaceNormal);
    void SeparateFromSurface(Vector3 offset);
}
