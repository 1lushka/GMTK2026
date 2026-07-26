using System.Collections;
using System.Collections.Generic;
using ForgettingBoxer.Knockout;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameplaySoundHooks : MonoBehaviour
{
    private readonly HashSet<HealthComponent> healthHooks = new();
    private readonly HashSet<ImpulseReceiver> impulseHooks = new();
    private readonly HashSet<PlayerAbilityManager> abilityHooks = new();
    private readonly HashSet<KnockoutSystem> knockoutHooks = new();

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(RefreshRoutine());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopAllCoroutines();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallHooks();
    }

    private IEnumerator RefreshRoutine()
    {
        while (enabled)
        {
            InstallHooks();
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private void InstallHooks()
    {
        HookHealth();
        HookImpulses();
        HookAbilities();
        HookKnockout();
        InstallWorldRelays();
    }

    private void HookHealth()
    {
        HealthComponent[] components = FindObjectsByType<HealthComponent>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (HealthComponent health in components)
        {
            if (health == null || !healthHooks.Add(health)) continue;
            health.onDamaged?.AddListener(damage => OnDamaged(health));
            health.onDeath?.AddListener(() => OnDeath(health));
        }
    }

    private static void OnDamaged(HealthComponent health)
    {
        if (health == null) return;
        SoundId id = health.GetComponent<BreakableProp>() != null
            ? SoundId.BreakablePropDamage
            : SoundId.CharacterDamage;
        SoundManager.PlayAt(id, health.transform.position);
    }

    private static void OnDeath(HealthComponent health)
    {
        if (health == null) return;
        SoundId id = health.GetComponent<BreakableProp>() != null
            ? SoundId.BreakablePropBreak
            : health.GetComponent<TrainingStand>() != null
                ? SoundId.TrainingStandDestroy
                : SoundId.CharacterDeath;
        SoundManager.PlayAt(id, health.transform.position);
    }

    private void HookImpulses()
    {
        ImpulseReceiver[] components = FindObjectsByType<ImpulseReceiver>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ImpulseReceiver receiver in components)
        {
            if (receiver == null || !impulseHooks.Add(receiver)) continue;
            receiver.ImpulseReceived += info =>
            {
                if (receiver != null && info.Force > 0f)
                    SoundManager.PlayAt(SoundId.ImpulseReceived, receiver.transform.position);
            };
        }
    }

    private void HookAbilities()
    {
        PlayerAbilityManager[] managers = FindObjectsByType<PlayerAbilityManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PlayerAbilityManager manager in managers)
        {
            if (manager == null || !abilityHooks.Add(manager)) continue;
            manager.AbilityActivated += OnAbilityActivated;
        }
    }

    private static void OnAbilityActivated(ActiveAbility ability)
    {
        switch (ability)
        {
            case DashAbility:
                SoundManager.Play(SoundId.DashWindup);
                SoundManager.Play(SoundId.DashLaunch);
                break;
            case ImpulseSpellAbility:
                SoundManager.Play(SoundId.ImpulseCast);
                SoundManager.Play(SoundId.ImpulseProjectileLaunch);
                break;
            case PunchComboAbility:
                SoundManager.Play(SoundId.PunchComboStart);
                break;
            case MagicGrabAbility:
                SoundManager.Play(SoundId.MagicGrabLaunch);
                break;
        }
    }

    private void HookKnockout()
    {
        KnockoutSystem[] systems = FindObjectsByType<KnockoutSystem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (KnockoutSystem system in systems)
        {
            if (system == null || !knockoutHooks.Add(system)) continue;
            system.onKnockoutStarted?.AddListener(() =>
            {
                SoundManager.Play(SoundId.KnockoutStart);
                SoundManager.Play(SoundId.KnockoutStarsScatter);
                StartCoroutine(CountdownSounds(system));
            });
            system.onRecovered?.AddListener(() => SoundManager.Play(SoundId.KnockoutRecovered));
            system.onGameOver?.AddListener(() => SoundManager.Play(SoundId.GameOver));
            system.onStarCountChanged?.AddListener(() =>
            {
                if (!system.IsKnockedOut) SoundManager.Play(SoundId.StarCollected);
            });
        }
    }

    private static void InstallWorldRelays()
    {
        AddRelay<ExplosiveProjectile, ExplosiveProjectileSoundRelay>();
        AddRelay<MagicGrabProjectile, MagicGrabProjectileSoundRelay>();
        AddRelay<FallTrap, FallTrapSoundRelay>();
        AddRelay<TrainingStandArm, TrainingStandArmSoundRelay>();
        AddRelay<ImpulseReceiver, ImpulseCollisionSoundRelay>();
        AddRelay<MagicGrabAbility, MagicGrabSoundRelay>();
        AddRelay<TopDownController, MovementSoundRelay>();
        AddRelay<TrainingStand, TrainingStandSpinSoundRelay>();
        AddRelay<PunchComboTrigger, PunchComboSoundRelay>();
    }

    private static IEnumerator CountdownSounds(KnockoutSystem system)
    {
        float interval = Mathf.Max(0.05f, system.RecoveryTimeLimit / 10f);
        while (system != null && system.IsKnockedOut)
        {
            SoundManager.Play(SoundId.KnockoutCountdownTick);
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    private static void AddRelay<TTarget, TRelay>()
        where TTarget : Component
        where TRelay : Component
    {
        TTarget[] targets = FindObjectsByType<TTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TTarget target in targets)
            if (target.GetComponent<TRelay>() == null) target.gameObject.AddComponent<TRelay>();
    }
}

public sealed class ExplosiveProjectileSoundRelay : MonoBehaviour
{
    private AudioSource loop;

    private void Start()
    {
        loop = SoundManager.PlayLoopAt(SoundId.ImpulseProjectileFlyLoop, transform.position);
    }

    private void LateUpdate()
    {
        if (loop != null) loop.transform.position = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        SoundManager.Stop(loop);
        SoundManager.PlayAt(SoundId.ImpulseExplosion, transform.position);
    }

    private void OnDestroy()
    {
        SoundManager.Stop(loop);
    }
}

public sealed class MagicGrabProjectileSoundRelay : MonoBehaviour
{
    private AudioSource loop;

    private void Start()
    {
        loop = SoundManager.PlayLoopAt(SoundId.MagicGrabFlyLoop, transform.position);
    }

    private void LateUpdate()
    {
        if (loop != null) loop.transform.position = transform.position;
    }

    private void OnDestroy()
    {
        SoundManager.Stop(loop);
        MagicGrabAbility ability = FindFirstObjectByType<MagicGrabAbility>();
        if (ability != null && ability.IsActive)
            SoundManager.PlayAt(SoundId.MagicGrabLatchMovable, transform.position);
        else
            SoundManager.PlayAt(SoundId.MagicGrabMiss, transform.position);
    }
}

public sealed class FallTrapSoundRelay : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<HealthComponent>() != null)
            SoundManager.PlayAt(SoundId.FallTrapTrigger, other.transform.position);
    }
}

public sealed class TrainingStandArmSoundRelay : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody body = GetComponentInParent<Rigidbody>();
        if (body != null && Mathf.Abs(body.angularVelocity.y) > 0.5f)
            SoundManager.PlayAt(SoundId.TrainingStandArmImpact, collision.GetContact(0).point);
    }
}

public sealed class ImpulseCollisionSoundRelay : MonoBehaviour
{
    private ImpulseReceiver receiver;

    private void Awake() => receiver = GetComponent<ImpulseReceiver>();

    private void OnCollisionEnter(Collision collision)
    {
        if (receiver == null || !receiver.IsMoving) return;
        SoundId id = collision.collider.GetComponentInParent<ImpulseReceiver>() != null
            ? SoundId.ImpulseCollisionMovable
            : SoundId.ImpulseCollisionSolid;
        SoundManager.PlayAt(id, collision.GetContact(0).point);
    }
}

public sealed class MagicGrabSoundRelay : MonoBehaviour
{
    private MagicGrabAbility ability;
    private AudioSource orbitLoop;
    private bool wasActive;

    private void Awake() => ability = GetComponent<MagicGrabAbility>();

    private void Update()
    {
        if (ability == null) return;
        if (ability.IsActive && !wasActive)
            orbitLoop = SoundManager.PlayLoopAt(SoundId.MagicGrabOrbitLoop, transform.position);
        else if (!ability.IsActive && wasActive)
        {
            SoundManager.Stop(orbitLoop);
            orbitLoop = null;
            SoundManager.Play(SoundId.MagicGrabRelease);
        }
        wasActive = ability.IsActive;
    }

    private void OnDisable() => SoundManager.Stop(orbitLoop);
}

public sealed class MovementSoundRelay : MonoBehaviour
{
    private TopDownController controller;
    private float nextStepTime;

    private void Awake() => controller = GetComponent<TopDownController>();

    private void Update()
    {
        if (controller == null || controller.CurrentVelocity.sqrMagnitude < 0.64f || Time.time < nextStepTime)
            return;

        float speed = controller.CurrentVelocity.magnitude;
        nextStepTime = Time.time + Mathf.Lerp(0.5f, 0.25f, Mathf.InverseLerp(0.8f, 6f, speed));
        SoundManager.PlayAt(SoundId.PlayerFootstep, transform.position);
    }
}

public sealed class TrainingStandSpinSoundRelay : MonoBehaviour
{
    private Rigidbody body;
    private AudioSource loop;

    private void Awake() => body = GetComponent<Rigidbody>();

    private void Update()
    {
        bool spinning = body != null && Mathf.Abs(body.angularVelocity.y) > 0.5f;
        if (spinning && loop == null)
            loop = SoundManager.PlayLoopAt(SoundId.TrainingStandSpinLoop, transform.position);
        else if (!spinning && loop != null)
        {
            SoundManager.Stop(loop);
            loop = null;
        }
    }

    private void OnDisable() => SoundManager.Stop(loop);
}

public sealed class PunchComboSoundRelay : MonoBehaviour
{
    private bool hitTarget;

    private void OnEnable() => hitTarget = false;

    private void OnTriggerEnter(Collider other)
    {
        HealthComponent health = other.GetComponentInParent<HealthComponent>();
        if (health == null || health.transform.IsChildOf(transform.root)) return;
        hitTarget = true;
        SoundManager.PlayAt(SoundId.PunchComboHit, other.ClosestPoint(transform.position));
        SoundManager.PlayAt(SoundId.PunchTargetLift, other.transform.position);
    }

    private void OnDisable()
    {
        SoundManager.Play(hitTarget ? SoundId.PunchComboFinalHit : SoundId.PunchComboMiss);
    }
}
