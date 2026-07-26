using UnityEngine;
using UnityEngine.SceneManagement;
using ForgettingBoxer.Knockout;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        if (KnockoutSystem.Instance != null)
            KnockoutSystem.Instance.onGameOver.AddListener(OnGameOver);
    }

    private void OnDestroy()
    {
        if (KnockoutSystem.Instance != null)
            KnockoutSystem.Instance.onGameOver.RemoveListener(OnGameOver);
    }

    private void OnGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    // Вызывается кнопкой Restart
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Вызывается кнопкой Main Menu
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}