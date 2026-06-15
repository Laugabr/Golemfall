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
    [SerializeField] private HoverDetailLabel xpLabel;   // Muestra solo el número de nivel
    [SerializeField] private Image xpFillBar;            // Image type: Filled

    private int cachedLevel = 1;
    // Para el detalle "Puntos Y" en hover (ver RefreshLabel):
    // private int cachedTotalXp = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start() => RefreshLabel();

    // Called by ExperienceManager.OnExperienceChanged via OnChangedRender
    public void UpdateXP(int totalXp, int currentLevel, int xpCurrentLevelStart, int xpNextLevel)
    {
        cachedLevel = currentLevel;
        // cachedTotalXp = totalXp; // descomentar para reactivar el detalle en hover

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
        if (xpLabel == null) return;

        // Por ahora solo mostramos el número de nivel, sin detalle en hover.
        xpLabel.Set(cachedLevel.ToString(), cachedLevel.ToString());

        // Para reactivar el detalle "Nivel X Puntos Y" en hover, comentar la línea
        // de arriba, descomentar esta y el campo/asignación de cachedTotalXp:
        // xpLabel.Set(cachedLevel.ToString(), $"Nivel {cachedLevel} Puntos {cachedTotalXp}");
    }
}