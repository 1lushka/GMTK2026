using UnityEngine;

namespace ForgettingBoxer.Knockout
{
    public sealed class KnockoutTestControls : MonoBehaviour
    {
        [SerializeField, Min(1)] private int starsToAdd = 1;
        [SerializeField, Min(1)] private int damage = 1;

        [ContextMenu("TEST / Add Stars")]
        public void TestAddStars() => KnockoutAPI.AddStars(starsToAdd);

        [ContextMenu("TEST / Take Damage")]
        public void TestTakeDamage() => KnockoutAPI.TakeDamage(damage);

        [ContextMenu("TEST / Reset Run")]
        public void TestResetRun()
        {
            if (KnockoutSystem.Instance != null) KnockoutSystem.Instance.ResetRun();
        }
    }
}
