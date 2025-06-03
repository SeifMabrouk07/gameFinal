using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Max hit points of the player")]
    public int maxHealth = 3;

    // Current health (starts at maxHealth). Other scripts can read this.
    [HideInInspector] public int currentHealth;

    // Fired whenever health changes: subscribers receive (currentHP, maxHP).
    public event Action<int, int> OnHealthChanged;

    // A flag so we only die once
    bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        // Fire once at start so UI shows full hearts
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Call this to reduce health (e.g. when slime attacks)
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        Debug.Log($"PlayerHealth: Took {amount} damage, now {currentHealth}/{maxHealth}");

        // Notify any UI or other listeners
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("PlayerHealth: Player died");
        // Play any death animation or trigger here, e.g.:
        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetTrigger("Die");

        // Optionally disable movement/collision, etc.
        GetComponent<Collider2D>().enabled = false;
        var pm = GetComponent<PlayerMovement>();
        if (pm != null)
            pm.enabled = false;

        // Destroy or handle Game Over after animation:
        AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(0);
        float dieLength = (clips.Length > 0) ? clips[0].clip.length : 0.5f;
        StartCoroutine(DestroyAfterDelay(dieLength));
    }

    System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // You could also load a Game Over scene instead of destroying:
        Destroy(gameObject);
    }
}