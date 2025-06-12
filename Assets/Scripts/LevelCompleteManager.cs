using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class LevelCompleteManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text Txt_LevelLabel;
    public TMP_Text Txt_RunPercent;
    public TMP_Text Txt_BestPercent;
    public Button Btn_Continue;

    [Header("Scene Names")]
    public string MainMenuScene = "MainMenu";

    void Start()
    {
        // Read from PlayerPrefs (set earlier by LevelProgressionManager)
        int level = PlayerPrefs.GetInt("CURRENT_LEVEL", 1);
        float runPct = PlayerPrefs.GetFloat("RUN_PERCENT", 0f);
        float bestPct = PlayerPrefs.GetFloat("BEST_PERCENT", 0f);

        if (Txt_LevelLabel != null)
            Txt_LevelLabel.text = $"Level {level} Complete!";

        if (Txt_RunPercent != null)
            Txt_RunPercent.text = $"This run: {runPct:F1}%";

        if (Txt_BestPercent != null)
            Txt_BestPercent.text = $"Your best: {bestPct:F1}%";

        if (Btn_Continue != null)
            Btn_Continue.onClick.AddListener(OnContinueClicked);
    }

    void OnContinueClicked()
    {
        SceneManager.LoadScene(MainMenuScene);
    }
}
