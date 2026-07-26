using UnityEngine;

public class LevelCompletion : MonoBehaviour
{
    public void Complete() => GameManager.Instance.OnLevelComplete();
}