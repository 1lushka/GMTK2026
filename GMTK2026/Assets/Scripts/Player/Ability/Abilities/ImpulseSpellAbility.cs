using System.Collections;
using UnityEngine;

public class ImpulseSpellAbility : ActiveAbility
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float spawnDistance = 0.5f;   // впереди игрока (по его forward)

    [Header("Explosion")]
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private GameObject explosionEffectPrefab;

    [Header("Timing")]
    [SerializeField] private float windUpTime = 0.2f;

    [Header("Animation")]
    [SerializeField] private string castTrigger = "CastSpell";
    [SerializeField] private bool useAnimator = true;

    private Animator animator;
    private Camera mainCamera;

    protected override void Start()
    {
        base.Start();
        mainCamera = Camera.main;
        if (useAnimator && controller != null)
            animator = controller.Animator;
    }

    protected override void Activate()
    {
        StartCoroutine(CastRoutine());
    }

    private IEnumerator CastRoutine()
    {
        if (animator != null)
            animator.SetTrigger(castTrigger);
        yield return new WaitForSeconds(windUpTime);
        SpawnProjectile();
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("ImpulseSpellAbility: projectile prefab is not assigned!");
            return;
        }

        Vector3 spawnPos = transform.position + transform.forward * spawnDistance;
        Vector3 flyDirection = GetMouseDirection();
        Quaternion spawnRotation = Quaternion.LookRotation(flyDirection);

        GameObject projectile = Instantiate(projectilePrefab, spawnPos, spawnRotation);

        Collider playerCollider = GetComponent<Collider>();
        Collider projCollider = projectile.GetComponent<Collider>();
        if (playerCollider != null && projCollider != null)
            Physics.IgnoreCollision(playerCollider, projCollider, true);

        ExplosiveProjectile projScript = projectile.GetComponent<ExplosiveProjectile>();
        if (projScript != null)
        {
            projScript.Initialize(projectileSpeed, explosionForce, explosionRadius, explosionEffectPrefab);
        }
        else
        {
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = flyDirection * projectileSpeed;
        }
    }

    private Vector3 GetMouseDirection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // плоскость Y=0

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 direction = targetPoint - transform.position;
            direction.y = 0f;   // <-- обеспечиваем горизонтальный полёт

            if (direction.sqrMagnitude < 0.001f)
                return transform.forward;

            return direction.normalized;
        }

        // Если не попали в плоскость — горизонтально вперёд
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        return forward.normalized;
    }
}