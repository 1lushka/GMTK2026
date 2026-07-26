using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerAbilityManager : MonoBehaviour
{
    [SerializeField] private List<AbilityDefinition> initialAbilities;
    private HashSet<string> activeAbilities = new HashSet<string>();
    public event Action<ActiveAbility> AbilityActivated;

    void Awake()
    {
        if (GameManager.Instance != null)
        {
            var initialIds = initialAbilities
                .Where(a => a.enabledByDefault)
                .Select(a => a.abilityId);
            GameManager.Instance.InitializeIfEmpty(initialIds);
            activeAbilities = new HashSet<string>(GameManager.Instance.GetActiveAbilities());
        }
        else
        {
            foreach (var ab in initialAbilities)
                if (ab.enabledByDefault) EnableAbility(ab.abilityId);
        }
    }

    public bool HasAbility(string abilityId) => activeAbilities.Contains(abilityId);
    public void EnableAbility(string abilityId) => activeAbilities.Add(abilityId);
    public void DisableAbility(string abilityId) => activeAbilities.Remove(abilityId);
    public void ToggleAbility(string abilityId) { if (HasAbility(abilityId)) DisableAbility(abilityId); else EnableAbility(abilityId); }
    public void EnableAbility(AbilityDefinition ability) { if (ability != null) EnableAbility(ability.abilityId); }
    public void DisableAbility(AbilityDefinition ability) { if (ability != null) DisableAbility(ability.abilityId); }
    public bool HasAbility(AbilityDefinition ability) => ability != null && HasAbility(ability.abilityId);
    public void NotifyAbilityActivated(ActiveAbility ability) => AbilityActivated?.Invoke(ability);
}