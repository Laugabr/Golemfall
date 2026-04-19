using UnityEngine;

/// <summary>
/// Maneja la experiencia y el nivel del jugador.
/// NO toca la UI directamente — dispara OnExperienceChanged para que GameHUD actualice las barras.
/// 
/// SETUP:
/// 1. Este script ya debería estar en un GameObject de tu escena (ej: "Managers" o el player)
/// 2. Asegurate de que tenga asignada la AnimationCurve de experiencia
/// 3. Asegurate de que tenga la lista de niveles configurada
/// 4. NO necesita referencias a UI — eso lo maneja GameHUD
/// 
/// TESTING:
/// - Mantené apretada la tecla G para ganar 4 XP por frame (para probar rápido)
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    [Header("Experience")]
    [SerializeField] private AnimationCurve experienceCurve;
    private int currentLevel = 1;
    [SerializeField] private int maxLevel = 10;

    private int totalExperience = 0;
    private int previousLevelsExperience = 0;
    private int nextLevelsExperience = 0;

    /// <summary>Se dispara cada vez que cambia XP o nivel. GameHUD escucha esto.</summary>
    public System.Action OnExperienceChanged;

    private void Awake()
    {
        BasicEventsManager.OnExperienceGain += AddExperience;
    }

    private void OnDestroy()
    {
        BasicEventsManager.OnExperienceGain -= AddExperience;
    }

    private void Start()
    {
        UpdateLevel();
    }

    private void Update()
    {
        // TEST: mantener G para ganar XP rápido
        if (Input.GetKey(KeyCode.G))
        {
            BasicEventsManager.OnExperienceGain?.Invoke(4);
        }
    }

    private void AddExperience(int amount)
    {
        totalExperience += amount;
        CheckForLevelUp();
        OnExperienceChanged?.Invoke();
    }

    private void CheckForLevelUp()
    {
        if (currentLevel >= maxLevel) return;
        if (totalExperience >= nextLevelsExperience)
        {
            currentLevel++;
            UpdateLevel();
        }
    }

    private void UpdateLevel()
    {
        previousLevelsExperience = (int)experienceCurve.Evaluate(currentLevel);
        nextLevelsExperience = (int)experienceCurve.Evaluate(currentLevel + 1);

        BasicEventsManager.OnLevelUp?.Invoke(currentLevel);
        OnExperienceChanged?.Invoke();
    }

    // ──────────────────────────────────────────────
    //  GETTERS PÚBLICOS (para GameHUD y CloudSaveGame)
    // ──────────────────────────────────────────────

    /// <summary>Nivel actual.</summary>
    public int GetCurrentLevel() => currentLevel;

    /// <summary>XP total acumulada (para guardar en Cloud Save).</summary>
    public int GetTotalExperience() => totalExperience;

    /// <summary>XP ganada dentro del nivel actual (para texto del HUD).</summary>
    public int GetCurrentLevelXP() => totalExperience - previousLevelsExperience;

    /// <summary>XP necesaria para pasar al siguiente nivel (para texto del HUD).</summary>
    public int GetXPToNextLevel() => nextLevelsExperience - previousLevelsExperience;

    /// <summary>Porcentaje de llenado de la barra de XP (0.0 a 1.0).</summary>
    public float GetXPFillAmount()
    {
        int xpToNext = GetXPToNextLevel();
        if (xpToNext <= 0) return 1f;
        return (float)GetCurrentLevelXP() / xpToNext;
    }

    /// <summary>Restaura nivel y XP desde Cloud Save.</summary>
    public void SetSavedData(int savedLevel, int savedXP)
    {
        currentLevel = Mathf.Clamp(savedLevel, 1, maxLevel);
        totalExperience = savedXP;

        previousLevelsExperience = (int)experienceCurve.Evaluate(currentLevel);
        nextLevelsExperience = (int)experienceCurve.Evaluate(currentLevel + 1);

        BasicEventsManager.OnLevelUp?.Invoke(currentLevel);
        OnExperienceChanged?.Invoke();

        Debug.Log($"[ExperienceManager] Restaurado: Nivel={currentLevel}, XP={totalExperience}");
    }
}