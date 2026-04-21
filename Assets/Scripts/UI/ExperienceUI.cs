using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

// Attach to a UI GameObject in the scene (not the player prefab).
// Finds the local player's ExperienceManager and displays their XP/level.
// Pattern mirrors PlayerStatsUI — only shows data for the InputAuthority player.

public class ExperienceUI : MonoBehaviour
{
    public static ExperienceUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText;      // e.g. "3"
    [SerializeField] private TextMeshProUGUI xpText;         // e.g. "45 XP"
    [SerializeField] private Image xpFillBar;                // Image type: Filled

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Called by ExperienceManager.OnExperienceChanged via OnChangedRender
    public void UpdateXP(int totalXp, int currentLevel, int xpCurrentLevelStart, int xpNextLevel)
    {
        int xpIntoLevel  = totalXp - xpCurrentLevelStart;
        int xpNeeded     = xpNextLevel - xpCurrentLevelStart;

        if (xpFillBar != null)
            xpFillBar.fillAmount = xpNeeded > 0 ? (float)xpIntoLevel / xpNeeded : 1f;

        if (xpText != null)
            xpText.text = $"{xpIntoLevel} XP";
    }

    // Called by ExperienceManager.OnLevelChanged via OnChangedRender
    public void UpdateLevel(int level)
    {
        if (levelText != null)
            levelText.text = $"{level}";
    }
}