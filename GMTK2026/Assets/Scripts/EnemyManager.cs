using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour
{
    public UnityEvent AllEnemiesDead;
    private int aliveCount;

    void Start()
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        aliveCount = enemies.Length;
        foreach (var enemy in enemies)
        {
            HealthComponent hc = enemy.GetComponent<HealthComponent>();
            if (hc != null) hc.onDeath.AddListener(OnEnemyDied);
        }
        if (aliveCount == 0) AllEnemiesDead?.Invoke();
    }

    void OnEnemyDied()
    {
        aliveCount--;
        if (aliveCount <= 0) AllEnemiesDead?.Invoke();
    }
}