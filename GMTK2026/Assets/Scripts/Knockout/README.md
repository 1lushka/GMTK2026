# Knockout module

Add one `KnockoutSystem` component to a scene and assign every field under **Scene references**. The module never creates its Canvas, counter, timer, status, or star visuals at runtime. The test scene contains an editable reference hierarchy named **Knockout UI (EDIT ME)**.

Call the module from any script:

```csharp
using ForgettingBoxer.Knockout;

KnockoutAPI.AddStar();
KnockoutAPI.AddStars(3);
KnockoutAPI.TakeDamage();
KnockoutAPI.TakeDamage(5);
```

`TakeDamage` ignores hits while the player is knocked out, invulnerable, or already defeated. Every accepted hit adds exactly one star, regardless of its damage value.

The inactive **Star Template (EDIT ME)** controls the appearance and click area of every animated star. **Collect Spawn Point (center)** controls where collected stars appear; **Landing + Launch Point** controls where they arrive and where they scatter from after damage. The counter uses a `CanvasGroup` and becomes invisible at zero stars.

The test scene is `Assets/Scenes/KnockoutTest.unity`. Unity upgrades an old generated version automatically after compilation. It can also be rebuilt through **Tools > Knockout > Create or Rebuild Test Scene**.

In Play Mode, select **Knockout System + Inspector Tests** and use **Add Stars**, **Take Damage**, and **Reset Run**.
