using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LevelEntry : MonoBehaviour
{
    public TextMeshProUGUI Txt_LevelName;
    public TextMeshProUGUI Txt_BestPercent;
    public Image Img_Lock;      // optional lock overlay
    public Button Btn_Level;

    int level;
    Action<int> onClick;
    Action<int> onLockedClick;

    /// <summary>
    /// Initialize this entry.
    /// </summary>
    public void Initialize(int level,
                           float bestPercent,
                           bool unlocked,
                           Action<int> onClick,
                           Action<int> onLockedClick)
    {
        this.level = level;
        this.onClick = onClick;
        this.onLockedClick = onLockedClick;

        Txt_LevelName.text = $"Level {level}";
        Txt_BestPercent.text = $"Best: {bestPercent:F1}%";
        Img_Lock.gameObject.SetActive(!unlocked);

        Btn_Level.onClick.RemoveAllListeners();
        if (unlocked)
            Btn_Level.onClick.AddListener(() => onClick(level));
        else
            Btn_Level.onClick.AddListener(() => onLockedClick(level));
    }
}
