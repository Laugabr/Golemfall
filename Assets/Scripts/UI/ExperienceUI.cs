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
    [SerializeField] private HoverDetailLabel xpLabel;   // Compacto: "1" | Hover: "Nivel 1 Puntos 93"
    [SerializeField] private Image xpFillBar;            // Image type: Filled

    // Valores cacheados: nivel y XP total llegan en callbacks distintos,
    // pero ambos son necesarios para armar el texto detallado.
    private int cachedLevel = 1;
    private int cachedTotalXp = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start() => RefreshLabel();

    // Called by ExperienceManager.OnExperienceChanged via OnChangedRender
    public void UpdateXP(int totalXp, int currentLevel, int xpCurrentLevelStart, int xpNextLevel)
    {
        cachedTotalXp = totalXp;
        cachedLevel = currentLevel;

        int xpIntoLevel = totalXp - xpCurrentLevelStart;
        int xpNeeded    = xpNextLevel - xpCurrentLevelStart;

        if (xpFillBar != null)
            xpFillBar.fillAmount = xpNeeded > 0 ? (float)xpIntoLevel / xpNeeded : 1f;

        RefreshLabel();
    }

    // Called by ExperienceManager.OnLevelChanged via OnChangedRender
    public void UpdateLevel(int level)
    {
        cachedLevel = level;
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (xpLabel != null)
            xpLabel.Set(cachedLevel.ToString(), $"Nivel {cachedLevel} Puntos {cachedTotalXp}");
    }
}