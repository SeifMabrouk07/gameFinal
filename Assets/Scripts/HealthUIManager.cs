using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Prefab for a single heart icon (Image)")]
    public GameObject heartIconPrefab;
    [Tooltip("PlayerHealth component reference")]
    public PlayerHealth playerHealth;
    [Tooltip("Parent transform (must have a Horizontal Layout Group)")]
    public Transform heartsContainer;

    List<Image> heartIcons = new List<Image>();

    void Start()
    {
        if (playerHealth == null)
        {
            Debug.LogError("HealthUIManager: PlayerHealth not assigned!");
            return;
        }

        if (heartIconPrefab == null)
        {
            Debug.LogError("HealthUIManager: heartIconPrefab not assigned!");
            return;
        }

        if (heartsContainer == null)
        {
            Debug.LogError("HealthUIManager: heartsContainer not assigned!");
            return;
        }

        // Subscribe to health‐changed event
        playerHealth.OnHealthChanged += UpdateHeartsDisplay;

        // Instantiate one heart for each maxHealth
        for (int i = 0; i < playerHealth.maxHealth; i++)
        {
            GameObject go = Instantiate(heartIconPrefab, heartsContainer);
            Image img = go.GetComponent<Image>();
            if (img != null)
                heartIcons.Add(img);
            else
                Debug.LogError("HeartIconPrefab has no Image component!");
        }

        // Initialize display (in case OnHealthChanged didn't fire in Awake)
        UpdateHeartsDisplay(playerHealth.currentHealth, playerHealth.maxHealth);
    }

    void UpdateHeartsDisplay(int currentHP, int maxHP)
    {
        // If maxHP changes at runtime, you should resize heartIcons list.
        for (int i = 0; i < heartIcons.Count; i++)
        {
            heartIcons[i].enabled = (i < currentHP);
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHeartsDisplay;
    }
}