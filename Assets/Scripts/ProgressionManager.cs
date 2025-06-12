using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelProgressionManager : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("This level’s index (1, 2, 3, …)")]
    public int levelNumber = 1;
    [Tooltip("Total enemies in this level (if left 0, it will auto-detect)")]
    public int totalEnemies = 0;

    [HideInInspector] public int kills;

    void Start()
    {
        // Auto-detect enemies if you haven't manually set totalEnemies
        if (totalEnemies <= 0)
            totalEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
    }

    /// <summary>
    /// Call this whenever an enemy dies.
    /// </summary>
    public void RegisterKill()
    {
        kills = Mathf.Min(kills + 1, totalEnemies);
    }

    /// <summary>
    /// Call this at the end of the level (victory or death).
    /// </summary>
    public void OnLevelEnd()
    {
        // Calculate percent completion
        float runPercent = totalEnemies > 0
            ? (kills / (float)totalEnemies) * 100f
            : 100f;

        int userId = PlayerPrefs.GetInt("USER_ID", -1);
        if (userId < 0)
        {
            Debug.LogWarning("No USER_ID found; cannot upload progress.");
            LoadLevelComplete(runPercent, runPercent);
            return;
        }

        // Upload to server, which will return the bestPercent
        NetworkManager.Instance.UpdateProgress(
            userId, levelNumber, runPercent,
            (success, err, bestPercent) =>
            {
                if (!success)
                {
                    Debug.LogError("Progress upload failed: " + err);
                    bestPercent = runPercent;
                }
                // Store for display in the next scene
                PlayerPrefs.SetFloat("RUN_PERCENT", runPercent);
                PlayerPrefs.SetFloat("BEST_PERCENT", bestPercent);
                PlayerPrefs.Save();

                LoadLevelComplete(runPercent, bestPercent);
            }
        );
    }

    void LoadLevelComplete(float runPct, float bestPct)
    {
        // You can pass via PlayerPrefs or a static GameManager
        SceneManager.LoadScene("LevelComplete");
    }
}
