using UnityEngine;

public class ExplosiveProjectile : MonoBehaviour
{
    private float speed;
    private float explosionForce;
    private float explosionRadius;
    private GameObject explosionEffectPrefab;
    private bool exploded;

    public void Initialize(float speed, float force, float radius, GameObject effectPrefab)
    {
        this.speed = speed;
        this.explosionForce = force;
        this.explosionRadius = radius;
        this.explosionEffectPrefab = effectPrefab;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = transform.forward * speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (exploded) return;
        Explode();
    }

    private void Explode()
    {
        exploded = true;

        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders)
        {
            // 1. »грок Ц отбрасывание (без урона)
            TopDownController playerCtrl = hit.GetComponent<TopDownController>();
            if (playerCtrl != null)
            {
                Vector3 dir = (playerCtrl.transform.position - transform.position).normalized;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f) dir = Random.insideUnitSphere;
                dir.y = 0f;
                dir.Normalize();
                playerCtrl.AddImpulse(dir * explosionForce);
                continue;
            }

            TrainingStand stand = hit.GetComponentInParent<TrainingStand>();
            if (stand != null)
            {
                stand.ApplyImpulseSpin(explosionForce, transform.position);
                continue;
            }

            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null)
            {
                Vector3 forceDir = (rb.position - transform.position).normalized;
                forceDir.y = 0f;
                if (forceDir.sqrMagnitude < 0.001f) forceDir = Random.insideUnitSphere;
                forceDir.y = 0f;
                forceDir.Normalize();
                rb.AddForce(forceDir * explosionForce, ForceMode.Impulse);
            }
        }

        Destroy(gameObject);
    }
}