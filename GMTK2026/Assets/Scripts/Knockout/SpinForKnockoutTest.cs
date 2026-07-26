using UnityEngine;

namespace ForgettingBoxer.Knockout
{
    public sealed class SpinForKnockoutTest : MonoBehaviour
    {
        private void Update() => transform.Rotate(0f, 35f * Time.deltaTime, 0f);
    }
}
