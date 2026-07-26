using UnityEngine;
using UnityEngine.UI;

public class AbilitySacrificeUI : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonsContainer;

    void Start()
    {
        if (GameManager.Instance == null) return;
        foreach (string abilityId in GameManager.Instance.GetActiveAbilities())
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonsContainer);
            Button btn = btnObj.GetComponent<Button>();
            btn.GetComponentInChildren<Text>().text = abilityId;
            btn.onClick.AddListener(() => OnSacrificeSelected(abilityId));
        }
    }

    void OnSacrificeSelected(string abilityId)
    {
        GameManager.Instance.RemoveAbility(abilityId);
        GameManager.Instance.ReturnToLevel();
    }
}