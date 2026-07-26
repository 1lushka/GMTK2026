# Knockout module

Drag `Assets/Prefabs/Systems/KnockoutSystem.prefab` into a scene. Its Canvas, counter, timer, star template, interaction layer, fallback EventSystem, and all internal references are already configured. Assign only `Player`, `Disable While Knocked Out`, and the UnityEvents required by the scene. If the scene already has an EventSystem, the prefab disables its fallback automatically.

The module never creates its gameplay UI at runtime. Open the prefab to edit the hierarchy named **Knockout UI (EDIT ME)**. Rebuild the default prefab through **Tools > Knockout > Create or Rebuild Production Prefab** when needed.

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
