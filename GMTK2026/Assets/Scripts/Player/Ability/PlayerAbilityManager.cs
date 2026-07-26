using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerAbilityManager : MonoBehaviour
{
    [SerializeField] private List<AbilityDefinition> initialAbilities;

    private HashSet<string> activeAbilities = new HashSet<string>();

    public event Action<ActiveAbility> AbilityActivated;

    void Awake()
    {
        foreach (var ability in initialAbilities)
        {
            if (ability.enabledByDefault)
                EnableAbility(ability.abilityId);
        }
    }

    public bool HasAbility(string abilityId) => activeAbilities.Contains(abilityId);

    public void EnableAbility(string abilityId)
    {
        activeAbilities.Add(abilityId);
        Debug.Log($"Способность '{abilityId}' включена");
    }

    public void DisableAbility(string abilityId)
    {
        activeAbilities.Remove(abilityId);
        Debug.Log($"Способность '{abilityId}' отключена");
    }

    public void ToggleAbility(string abilityId)
    {
        if (HasAbility(abilityId)) DisableAbility(abilityId);
        else EnableAbility(abilityId);
    }

    public void EnableAbility(AbilityDefinition ability) { if (ability != null) EnableAbility(ability.abilityId); }
    public void DisableAbility(AbilityDefinition ability) { if (ability != null) DisableAbility(ability.abilityId); }
    public bool HasAbility(AbilityDefinition ability) => ability != null && HasAbility(ability.abilityId);

    public void NotifyAbilityActivated(ActiveAbility ability)
    {
        AbilityActivated?.Invoke(ability);
    }
}
