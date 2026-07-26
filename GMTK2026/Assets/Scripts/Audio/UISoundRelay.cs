using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UISoundRelay : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerClickHandler
{
    public static void InstallForScene()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
            if (button.GetComponent<UISoundRelay>() == null) button.gameObject.AddComponent<UISoundRelay>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsInteractable()) SoundManager.Play(SoundId.UIButtonHover);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsInteractable()) SoundManager.Play(SoundId.UIButtonPress);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        SoundManager.Play(name == "Knockout Star" ? SoundId.KnockoutStarClick : SoundId.UIButtonConfirm);
    }

    private bool IsInteractable()
    {
        Selectable selectable = GetComponent<Selectable>();
        return selectable != null && selectable.IsInteractable();
    }
}
