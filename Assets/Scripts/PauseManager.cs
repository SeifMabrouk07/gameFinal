using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject PausePanel;       // drag the PausePanel here
    public TMP_Text Txt_Progress;     // the progress text
    public Button Btn_Resume;       // optional
    public Button Btn_Quit;         // optional

    LevelProgressionManager lvlProg;
    bool isPaused = false;

    void Start()
    {
        // find the progression manager in the scene
        lvlProg = FindObjectOfType<LevelProgressionManager>();

        // hook up buttons
        if (Btn_Resume != null)
            Btn_Resume.onClick.AddListener(Resume);

        if (Btn_Quit != null)
            Btn_Quit.onClick.AddListener(() =>
                SceneManager.LoadScene("MainMenu")
            );

        PausePanel.SetActive(false);
    }


    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }
    void Update()
    {
        // toggle on Escape (for desktop) or a mobile pause button mapped to “Cancel”
        if (Input.GetButtonDown("Cancel"))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        if (lvlProg == null) return;
        isPaused = true;
        Time.timeScale = 0f;

        // compute progress
        int kills = lvlProg.kills;
        int total = lvlProg.totalEnemies;
        float pct = total > 0
            ? (kills / (float)total) * 100f
            : 100f;

        // update UI
        Txt_Progress.text = $"Progress: {pct:F1}% ({kills}/{total})";
        PausePanel.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        PausePanel.SetActive(false);
    }
}
