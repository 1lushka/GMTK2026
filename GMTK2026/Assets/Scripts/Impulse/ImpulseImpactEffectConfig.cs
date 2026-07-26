using UnityEngine;

[CreateAssetMenu(fileName = "ImpulseImpactEffectConfig", menuName = "GMTK/Impulse Impact Effect Config")]
public sealed class ImpulseImpactEffectConfig : ScriptableObject
{
    [SerializeField] private GameObject effectPrefab;
    [SerializeField, Min(0f)] private float effectLifetime = 2f;
    [SerializeField] private bool alignWithImpactDirection = true;

    public void Spawn(Vector3 position, Vector2 direction)
    {
        if (effectPrefab == null) return;

        Quaternion rotation = Quaternion.identity;
        if (alignWithImpactDirection && direction.sqrMagnitude > 0f)
        {
            Vector3 forward = new Vector3(direction.x, 0f, direction.y);
            rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        GameObject instance = Instantiate(effectPrefab, position, rotation);
        if (effectLifetime > 0f)
            Destroy(instance, effectLifetime);
    }
}
