using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

// Lives on the player prefab alongside PlayerHealth and PlayerStats.
// Only the server modifies TotalExperience and CurrentLevel.
// UI updates locally via OnChangedRender, visible only to the InputAuthority.

public class ExperienceManager : NetworkBehaviour
{
    [Header("Experience Curve")]
    [SerializeField] private AnimationCurve experienceCurve;
    [SerializeField] private int maxLevel = 10;

    [Header("XP per GameEvent (individual)")]
    [SerializeField] private int xpPerKillEnemy = 20;
    [SerializeField] private int xpPerBreakBreakable = 3;
    [SerializeField] private int xpPerCollectItem = 5;
    [SerializeField] private int xpPerCollectSpecial = 10;

    // Networked state — only the server writes these
    [Networked, OnChangedRender(nameof(OnExperienceChanged))]
    public int TotalExperience { get; set; }

    [Networked, OnChangedRender(nameof(OnLevelChanged))]
    public int CurrentLevel { get; set; }

    //  Lifecycle 

    public override void Spawned()
    {
        // === DIAGNÓSTICO (corre en TODOS los peers, antes del filtro de autoridad) ===
        Debug.Log($"[XP] Spawned | Nivel={CurrentLevel} XP={TotalExperience} InputAuth={Object.HasInputAuthority} StateAuth={Object.HasStateAuthority}");

        if (!Object.HasStateAuthority) return;

        CurrentLevel = 1;
        TotalExperience = 0;

        var missionController = FindFirstObjectByType<MissionController>();
        if (missionController == null)
            Debug.LogError("[XP CatchUp] NO se encontró MissionController");
        else
            Debug.Log($"[XP CatchUp] MissionController encontrado. GroupXpAwarded = {missionController.GroupXpAwarded}");

        if (missionController != null && missionController.GroupXpAwarded > 0)
        {
            Debug.Log($"[XP CatchUp] Aplicando {missionController.GroupXpAwarded} XP al que se une");
            AddExperience(missionController.GroupXpAwarded);
            Debug.Log($"[XP CatchUp] Tras aplicar: Nivel={CurrentLevel}, XP={TotalExperience}");
        }
        else
        {
            Debug.LogWarning("[XP CatchUp] No se aplicó XP (controller null o GroupXpAwarded = 0)");
        }

        TrackEvents.OnTrackEvent += ServerHandleEvent;
    }

    private void OnDestroy()
    {
        TrackEvents.OnTrackEvent -= ServerHandleEvent;
    }

    // Server: individual XP from gameplay events 

    private void ServerHandleEvent(GameEventType eventType, int amount, string key)
    {
        if (!Object.HasStateAuthority) return;

        if (eventType == GameEventType.KillEnemy || eventType == GameEventType.BreakBreakable)
            return;

        if (!Object.HasInputAuthority) return;

        int xp = GetXpForEvent(eventType) * amount;
        if (xp > 0) AddExperience(xp);
    }

    private int GetXpForEvent(GameEventType eventType)
    {
        return eventType switch
        {
            GameEventType.KillEnemy => xpPerKillEnemy,
            GameEventType.BreakBreakable => xpPerBreakBreakable,
            GameEventType.CollectItem => xpPerCollectItem,
            GameEventType.CollectSpecialItem => xpPerCollectSpecial,
            _ => 0
        };
    }

    // Server: add XP (called from MissionController for group XP too)

    public void AddExperience(int amount)
    {
        if (!Object.HasStateAuthority) return;
        if (CurrentLevel >= maxLevel) return;

        TotalExperience += amount;
        CheckLevelUp();
    }

    // RPC so clients can request XP gain for individual events
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ServerAddExperience(int amount, RpcInfo info = default)
    {
        AddExperience(amount);
    }

    // Kill grupal
    public static void GrantKillXpToAll()
    {
        var all = FindObjectsByType<ExperienceManager>(FindObjectsSortMode.None);
        foreach (var em in all)
            em.AddExperience(em.xpPerKillEnemy);
    }

    // Break individual
    public void GrantBreakXp()
    {
        AddExperience(xpPerBreakBreakable);
    }

    // Server: level-up logic 

    private void CheckLevelUp()
    {
        if (!Object.HasStateAuthority) return;
        if (CurrentLevel >= maxLevel) return;

        int xpForNext = GetXpForLevel(CurrentLevel + 1);

        if (TotalExperience >= xpForNext)
        {
            CurrentLevel++;

            BasicEventsManager.OnLevelUp?.Invoke(CurrentLevel);
            GetComponent<PlayerProgressionVisuals>()?.OnPlayerLevelUp(CurrentLevel);

            CheckLevelUp();
        }
    }

    private int GetXpForLevel(int level)
    {
        return (int)experienceCurve.Evaluate(level);
    }

    // Render callbacks (run on all clients when networked values change)

    private void OnExperienceChanged()
    {
        // === DIAGNÓSTICO ===
        Debug.Log($"[XP] OnExperienceChanged | Nivel={CurrentLevel} XP={TotalExperience} InputAuth={Object.HasInputAuthority}");

        if (!Object.HasInputAuthority) return;
        ExperienceUI.Instance?.UpdateXP(TotalExperience, CurrentLevel, GetXpForLevel(CurrentLevel), GetXpForLevel(CurrentLevel + 1));
    }

    private void OnLevelChanged()
    {
        if (!Object.HasInputAuthority) return;
        ExperienceUI.Instance?.UpdateLevel(CurrentLevel);
    }

    public static void GrantMissionXpToAll(int xpAmount)
    {
    }
}