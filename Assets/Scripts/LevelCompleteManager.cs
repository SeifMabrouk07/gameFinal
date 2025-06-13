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
        int level = PlayerPrefs.GetInt("CURRENT_LEVEL", 1);
        float runPct = PlayerPrefs.GetFloat("RUN_PERCENT", 0f);
        float bestPct = PlayerPrefs.GetFloat("BEST_PERCENT", 0f);

        // Only show “Level # Complete!” if the player actually got 100 %
        if (runPct >= 100f)
        {
            Txt_LevelLabel.text = $"Level {level} Complete!";
        }
        else
        {
            Txt_LevelLabel.text = "You Died";
        }

        // Always show the run % and best % beneath
        Txt_RunPercent.text = $"This run: {runPct:F1}%";
        Txt_BestPercent.text = $"Your best: {bestPct:F1}%";

        Btn_Continue.onClick.AddListener(OnContinueClicked);
    }

    void OnContinueClicked()
    {
        SceneManager.LoadScene(MainMenuScene);
    }
}
