using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AbilitySacrificeButton : MonoBehaviour
{
    [SerializeField] private string abilityId;
    [SerializeField] private Button button;
    //[SerializeField] private Text label;

    private void Start()
    {
        if (button == null) button = GetComponent<Button>();
        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.GetActiveAbilityIds().Contains(abilityId))
            {
                gameObject.SetActive(false);
                return;
            }

            AbilityDefinition def = GameManager.Instance.GetAbilityDefinition(abilityId);
            //if (def != null && label != null)
            //{
            //    label.text = $"<b>{def.displayName}</b>\n<size=18>{def.description}</size>";
            //}

            button.onClick.AddListener(OnSacrificeClicked);
        }
    }

    private void OnSacrificeClicked()
    {
        GameManager.Instance.RemoveAbility(abilityId);
        GameManager.Instance.ReturnToLevel();
    }
}