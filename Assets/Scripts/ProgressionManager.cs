using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelProgressionManager : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("This level’s index (1, 2, 3, …)")]
    public int levelNumber = 1;

    [Tooltip("Total enemies in this level (0 = auto-detect)")]
    public int totalEnemies = 0;

    [HideInInspector] public int kills;

    void Start()
    {
        if (totalEnemies <= 0)
            totalEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        kills = 0;
    }

    public void RegisterKill()
    {
        kills = Mathf.Min(kills + 1, totalEnemies);
        Debug.Log($"Progression: killed {kills}/{totalEnemies}");
        if (kills >= totalEnemies)
        {
            Debug.Log("All enemies down — ending level.");
            OnLevelEnd();
        }
    }

    public void OnLevelEnd()
    {
        float runPercent = totalEnemies > 0
            ? (kills / (float)totalEnemies) * 100f
            : 100f;

        PlayerPrefs.SetInt("CURRENT_LEVEL", levelNumber);
        PlayerPrefs.SetFloat("RUN_PERCENT", runPercent);

        int userId = PlayerPrefs.GetInt("USER_ID", -1);

        // If no user or no NetworkManager, skip server upload
        if (userId < 0 || NetworkManager.Instance == null)
        {
            PlayerPrefs.SetFloat("BEST_PERCENT", runPercent);
            PlayerPrefs.Save();
            SceneManager.LoadScene("LevelComplete");
            return;
        }

        // Otherwise, upload and get back the true bestPercent
        NetworkManager.Instance.UpdateProgress(
            userId,
            levelNumber,
            runPercent,
            (success, err, bestPercent) =>
            {
                if (!success)
                {
                    Debug.LogError("Failed to update progress: " + err);
                    bestPercent = runPercent;
                }

                PlayerPrefs.SetFloat("BEST_PERCENT", bestPercent);
                PlayerPrefs.Save();
                SceneManager.LoadScene("LevelComplete");
            }
        );
    }
}
