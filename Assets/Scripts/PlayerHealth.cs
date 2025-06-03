// PlayerHealth.cs
using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 3;
    public int currentHP { get; private set; }

    // This event fires whenever currentHP changes
    public event Action<int, int> OnHealthChanged;
    // (sender will pass (currentHP, maxHP))

    void Awake()
    {
        currentHP = maxHP;
        // Immediately notify UI of starting value
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(currentHP - amount, 0);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    void Die()
    {
        Debug.Log("Player died!");
        // Your death logic here (reload scene, play animation, etc.)
    }
}
