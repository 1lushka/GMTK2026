using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Level1";

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}