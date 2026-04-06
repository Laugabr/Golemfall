using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : CharacterStats
{
    [Networked] public NetworkBool DirtyStats { get; set; }
    public UnityEvent OnStatsChanged;
    public List<Stats> levelsData = new();

    // Lista para guardar los puntos ganados por subir de nivel
    private List<StatInfo> baseLevelStats = new();

    private void OnEnable() => BasicEventsManager.OnLevelUp += HandleLevelUp;

    private void OnDisable() => BasicEventsManager.OnLevelUp -= HandleLevelUp;
    private void HandleLevelUp(int levelId)
    {
        if (!Object.HasStateAuthority) return;

        var levelSO = levelsData[levelId - 1];
        foreach (var info in levelSO.statInfo)
        {
            var existing = baseLevelStats.FirstOrDefault(s => s.statType == info.statType);
            if (existing != null) existing.statValue += info.statValue;
            else baseLevelStats.Add(new StatInfo(info.statType, info.statValue));
            OnStatsChanged?.Invoke();
        }

        RefreshStats(); // Recalcular todo con el nuevo nivel
    }

    public void RefreshStats()
    {
        if (!Object.HasStateAuthority) return;

        if (baseLevelStats.Count == 0)
            baseLevelStats = baseStats.statInfo.Select(s => new StatInfo(s.statType, s.statValue)).ToList();

        localStats.Clear();
        foreach (var bs in baseLevelStats)
        {
            var existing = localStats.FirstOrDefault(s => s.statType == bs.statType);
            if (existing != null) existing.statValue += bs.statValue;
            else localStats.Add(new StatInfo(bs.statType, bs.statValue));
        }

        var inv = GetComponent<NetworkInventory>();
        foreach (short key in inv.EquippedItems)
        {
            ItemData data = ResourcesManager.instance.inventoryItemBank.GetValue(key) as ItemData;
            if (data != null && data.stats != null)
                ApplyModifier(data.stats);
        }

        DirtyStats = true;
    }

    private void ApplyModifier(Stats modifier)
    {
        if (!Object.HasStateAuthority) return;

        foreach (var mod in modifier.statInfo)
        {
            var stat = localStats.FirstOrDefault(s => s.statType == mod.statType);
            if (stat != null) stat.statValue += mod.statValue;
            else localStats.Add(new StatInfo(mod.statType, mod.statValue));
        }
    }

    public override void Render()
    {
        if (DirtyStats)
        {
            DirtyStats = false;
            OnStatsChanged?.Invoke();
            DebugStats("CLIENT UPDATE");
        }
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            Initialize();
    }

    // --- Helpers de Debug ---
    public void DebugStats(string origin)
    {
        string s = $"[{origin}] Stats Actuales: ";
        foreach (var stat in localStats) s += $"{stat.statType}:{stat.statValue} | ";
        Debug.Log(s);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) DebugStats(Object.HasStateAuthority ? "SERVER" : "CLIENT");
    }
}