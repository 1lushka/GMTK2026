# Controlled impulse

Add `ImpulseReceiver` to an object with a non-kinematic `Rigidbody`, then assign an `ImpulseProfile`. A ready starting profile is available at `Assets/SO/ImpulseProfiles/DefaultImpulseProfile.asset`.

```csharp
ImpulseReceiver receiver = target.GetComponentInParent<ImpulseReceiver>();
receiver?.ApplyImpulse(direction, force, gameObject);
```

`Vector2.x` maps to world X and `Vector2.y` maps to world Z. Initial added speed is `force * SpeedPerForce`, clamped together with the existing velocity to `MaxSpeed`.

The receiver moves at constant velocity during `FlightTime`, decelerates to zero during `DecelerationTime`, and rejects movement impulses during `StopLockDuration`. It still invokes `ImpulseReceived` and the Inspector event while locked or when its profile is immovable.

Receiver collisions transfer an impulse away from the source object's centre. A wall, an immovable receiver, or a temporarily locked receiver stops and locks the flying object. This prevents repeated transfer loops when objects accumulate against a wall.
