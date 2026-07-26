# AGENTS.md

## Unity Code Rules

Write simple, clean and reliable Unity C# code.

### Simplicity

* Prefer the simplest solution that solves the task.
* Do not over-engineer.
* Do not create unnecessary interfaces, managers, services or abstractions.
* Prefer composition over inheritance.

### Small scripts

* One script = one clear responsibility.
* Prefer scripts under ~250 lines.
* Prefer methods under ~30 lines.
* Split large MonoBehaviours when responsibilities become mixed.

### Unity

* Use `[SerializeField] private` instead of public Inspector fields.
* Cache `GetComponent` references.
* Avoid `Find`, `GetComponent`, LINQ and allocations inside `Update`.
* Keep `Update`, `FixedUpdate` and `LateUpdate` lightweight.
* Use `FixedUpdate` for physics when appropriate.
* Unsubscribe from events in `OnDisable`/`OnDestroy`.
* Do not put `UnityEditor` code in runtime scripts.

### Architecture

* Keep gameplay logic separate from UI when practical.
* Prefer plain C# classes for logic that does not need Unity APIs.
* Do not create global static state or Singletons unless genuinely necessary.
* Reuse existing project systems instead of creating duplicates.

### Safety

* Do not rename serialized fields unnecessarily.
* Do not break existing prefabs, scenes or serialized references.
* Do not modify unrelated files.
* Never delete or regenerate `.meta` files unnecessarily.

### Before coding

First inspect the existing relevant code and reuse its patterns.

Make the smallest change that fully solves the task.

### Before finishing

Check:

* compilation errors
* null-reference risks
* duplicated code
* oversized methods/scripts
* unnecessary complexity
* allocations in hot paths
* temporary Debug.Log calls
* broken event subscriptions

Fix discovered problems before finishing.

Never claim something was tested unless it was actually tested.
