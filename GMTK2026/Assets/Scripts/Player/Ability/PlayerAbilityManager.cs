using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilityManager : MonoBehaviour
{
    [SerializeField] private List<AbilityDefinition> initialAbilities;
    private HashSet<string> activeAbilityIds = new HashSet<string>();
    public event Action<ActiveAbility> AbilityActivated;

    void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.InitializeAbilities(initialAbilities);
            activeAbilityIds = new HashSet<string>(GameManager.Instance.GetActiveAbilityIds());
        }
        else
        {
            foreach (var ab in initialAbilities)
                if (ab.enabledByDefault) activeAbilityIds.Add(ab.abilityId);
        }
    }

    public bool HasAbility(string abilityId) => activeAbilityIds.Contains(abilityId);
    public void EnableAbility(string abilityId) => activeAbilityIds.Add(abilityId);
    public void DisableAbility(string abilityId) => activeAbilityIds.Remove(abilityId);
    public void ToggleAbility(string abilityId)
    {
        if (HasAbility(abilityId)) DisableAbility(abilityId);
        else EnableAbility(abilityId);
    }
    public void EnableAbility(AbilityDefinition ability) { if (ability != null) EnableAbility(ability.abilityId); }
    public void DisableAbility(AbilityDefinition ability) { if (ability != null) DisableAbility(ability.abilityId); }
    public bool HasAbility(AbilityDefinition ability) => ability != null && HasAbility(ability.abilityId);
    public void NotifyAbilityActivated(ActiveAbility ability) => AbilityActivated?.Invoke(ability);
}