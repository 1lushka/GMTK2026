using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;
    private bool isStunned;

    [System.Serializable]
    public class DamagedEvent : UnityEvent<int> { }  
    public UnityEvent onDeath;
    public DamagedEvent onDamaged;                 

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        onDamaged?.Invoke(damage);   

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            onDeath?.Invoke();
        }
    }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsStunned => isStunned;

    public void SetStunned(bool stunned)
    {
        isStunned = stunned;
    }
}
