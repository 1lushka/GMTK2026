using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgettingBoxer.Knockout
{
    [DisallowMultipleComponent]
    public sealed class KnockoutEventSystemGuard : MonoBehaviour
    {
        [SerializeField] private EventSystem localEventSystem;

        private void Awake()
        {
            if (localEventSystem == null)
                localEventSystem = GetComponent<EventSystem>();

            EventSystem[] systems = FindObjectsByType<EventSystem>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (EventSystem system in systems)
            {
                if (system == localEventSystem) continue;
                gameObject.SetActive(false);
                return;
            }
        }
    }
}
