using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private string sacrificeSceneName = "SacrificeScene";

    private HashSet<string> activeAbilities = new HashSet<string>();
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

    public void InitializeIfEmpty(IEnumerable<string> abilityIds)
    {
        if (activeAbilities.Count == 0)
        {
            activeAbilities = new HashSet<string>(abilityIds);
        }
    }

    public IEnumerable<string> GetActiveAbilities() => activeAbilities;
    public void RemoveAbility(string abilityId) => activeAbilities.Remove(abilityId);

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