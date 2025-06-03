// HealthUIManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthUIManager : MonoBehaviour
{
    [Header("Assign at Runtime or in Inspector")]
    [Tooltip("Drag in the HeartIcon prefab here")]
    public GameObject heartIconPrefab;

    [Tooltip("Drag in your PlayerHealth component (e.g. the Player GameObject)")]
    public PlayerHealth playerHealth;

    [Tooltip("Parent object under the Canvas where hearts will appear")]
    public Transform heartsContainer;

    // Keep track of the instantiated heart icons
    List<Image> heartIcons = new List<Image>();

    void Start()
    {
        if (playerHealth == null)
        {
            Debug.LogError("HealthUIManager: PlayerHealth not assigned!", this);
            return;
        }

        if (heartIconPrefab == null)
        {
            Debug.LogError("HealthUIManager: heartIconPrefab not assigned!", this);
            return;
        }

        if (heartsContainer == null)
        {
            Debug.LogError("HealthUIManager: heartsContainer not assigned!", this);
            return;
        }

        // Subscribe to health changes
        playerHealth.OnHealthChanged += UpdateHeartsDisplay;

        // Build initial heart icons equal to maxHP
        for (int i = 0; i < playerHealth.maxHP; i++)
        {
            GameObject go = Instantiate(heartIconPrefab, heartsContainer);
            // Ensure the instantiated object is an Image
            Image img = go.GetComponent<Image>();
            if (img != null)
            {
                heartIcons.Add(img);
            }
            else
            {
                Debug.LogError("HeartIcon prefab doesn't have an Image component on the root!", go);
            }
        }

        // Immediately set the correct initial visibility
        UpdateHeartsDisplay(playerHealth.currentHP, playerHealth.maxHP);
    }

    void UpdateHeartsDisplay(int currentHP, int maxHP)
    {
        // If maxHP ever changes, we could resize the list, but
        // here we assume maxHP is fixed at Start.

        // Loop over each icon: if its index < currentHP, show it, otherwise hide it
        for (int i = 0; i < heartIcons.Count; i++)
        {
            heartIcons[i].enabled = (i < currentHP);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from the event to avoid null‐refs in editor / playmode exit
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHeartsDisplay;
    }
}
