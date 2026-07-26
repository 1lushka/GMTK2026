using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private string sacrificeSceneName = "SacrificeScene";

    private HashSet<string> activeAbilityIds = new HashSet<string>();
    private Dictionary<string, AbilityDefinition> abilityDefs = new Dictionary<string, AbilityDefinition>();
    private string currentLevelName;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void InitializeAbilities(IEnumerable<AbilityDefinition> abilities)
    {
        activeAbilityIds.Clear();
        abilityDefs.Clear();
        foreach (var def in abilities)
        {
            if (def.enabledByDefault)
            {
                activeAbilityIds.Add(def.abilityId);
                abilityDefs[def.abilityId] = def;
            }
        }
    }

    public IEnumerable<string> GetActiveAbilityIds() => activeAbilityIds;
    public AbilityDefinition GetAbilityDefinition(string abilityId)
    {
        abilityDefs.TryGetValue(abilityId, out var def);
        return def;
    }

    public void RemoveAbility(string abilityId) => activeAbilityIds.Remove(abilityId);

    public void OnLevelComplete()
    {
        currentLevelName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sacrificeSceneName);
    }

    public void ReturnToLevel()
    {
        if (string.IsNullOrEmpty(currentLevelName))
            currentLevelName = "GameLevel";
        SceneManager.LoadScene(currentLevelName);
    }
}