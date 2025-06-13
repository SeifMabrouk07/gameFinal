using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public List<LevelEntry> levelEntries;   // drag each LevelEntry instance here
    public TextMeshProUGUI Txt_Message;    // bottom‐center message

    [Header("Settings")]
    public string levelScenePrefix = "Level"; // Level1, Level2…

    void Start()
    {
        // Basic wiring check
        if (levelEntries == null || levelEntries.Count == 0)
            Debug.LogError("Please assign your LevelEntry instances in the inspector!");

        int userId = PlayerPrefs.GetInt("USER_ID", -1);
        if (userId < 0)
        {
            SceneManager.LoadScene("SignIn");
            return;
        }

        StartCoroutine(InitializeEntries(userId));
    }

    IEnumerator InitializeEntries(int userId)
    {
        int maxLevels = levelEntries.Count;
        var bests = new float[maxLevels];

        // 1) Fetch best% for each level
        for (int i = 0; i < maxLevels; i++)
        {
            int level = i + 1;
            bool done = false;
            NetworkManager.Instance.GetProgress(
                userId, level,
                (ok, err, bp) =>
                {
                    if (!ok) Debug.LogWarning($"L{level} progress error: {err}");
                    bests[i] = bp;
                    done = true;
                }
            );
            yield return new WaitUntil(() => done);
        }

        // 2) Configure each entry
        for (int i = 0; i < maxLevels; i++)
        {
            int level = i + 1;
            var entry = levelEntries[i];
            float best = bests[i];
            bool unlocked = (level == 1) || (bests[i - 1] >= 100f);

            entry.Initialize(
                level,
                best,
                unlocked,
                OnLevelSelected,
                OnLevelLocked
            );
        }
    }

    void OnLevelSelected(int level)
    {
        SceneManager.LoadScene($"{levelScenePrefix}{level}");
    }

    void OnLevelLocked(int level)
    {
        int prev = level - 1;
        Txt_Message.text = $"You must complete Level {prev} to 100% to unlock Level {level}.";
        StartCoroutine(ClearMessageAfterDelay(3f));
    }

    IEnumerator ClearMessageAfterDelay(float t)
    {
        yield return new WaitForSeconds(t);
        Txt_Message.text = "";
    }
}
