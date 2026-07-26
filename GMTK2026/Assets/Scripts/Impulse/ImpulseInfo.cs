using UnityEngine;

public readonly struct ImpulseInfo
{
    public ImpulseInfo(Vector2 direction, float force, GameObject source)
    {
        Direction = direction;
        Force = force;
        Source = source;
    }

    public Vector2 Direction { get; }
    public float Force { get; }
    public GameObject Source { get; }
}
