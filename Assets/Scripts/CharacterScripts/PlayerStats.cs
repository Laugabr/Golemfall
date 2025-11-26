using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

// Manages player's stats, level-ups, and equipment effects with network syncing
public class PlayerStats : CharacterStats
{
    [Networked] public NetworkBool DirtyStats { get; set; }

    [Header("Events")]
    public UnityEvent OnStatsChanged;

    public List<Stats> levelsData = new();

    private void OnEnable()
    {
        BasicEventsManager.OnLevelUp += HandleLevelUp;

        if (EquipManager.Instance != null)
            EquipManager.Instance.OnEquipChanged += HandleEquipChange;
    }

    private void OnDisable()
    {
        if (EquipManager.Instance != null)
            EquipManager.Instance.OnEquipChanged -= HandleEquipChange;
    }

    // Handles level-up: adds stats from the levelData
    private void HandleLevelUp(int levelId)
    {
        if (!Object.HasStateAuthority) return;

        var currentLevel = levelsData[levelId - 1];

        foreach (var levelStat in currentLevel.statInfo)
        {
            var stat = localStats.FirstOrDefault(s => s.statType == levelStat.statType);

            if (stat != null)
            {
                stat.statValue += levelStat.statValue;
            }
            else
            {
                localStats.Add(new StatInfo(levelStat.statType, levelStat.statValue));
            }

            if (stat.statValue < 0)
                stat.statValue = 0;
        }
        if (Object.HasStateAuthority)
        {
            DebugStats("SERVER");
        }
        DirtyStats = true;
    }


    private void HandleEquipChange(ItemData itemData, bool isEquiped)
    {

    }

    // RPC to equip an item and apply its stats
    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
    public void RPC_EquipItem(string itemID)
    {
        if (!Object.HasStateAuthority) return;

        var itemStats = Resources.Load<Stats>($"DataSO/StatsData/Itemscrafteados/Stats_{itemID}");

        if (itemStats == null)
        {
            Debug.LogError($"Stats_{itemID} no encontrado en Resources.");
            return;
        }
        if (Object.HasStateAuthority)
        {
            DebugStats("SERVER");
        }
        EquipItem_Server(itemStats);
        DirtyStats = true;
    }

    // RPC to unequip an item and remove its stats
    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
    public void RPC_UnequipItem(string itemID)
    {
        if (!Object.HasStateAuthority) return;

        var itemStats = Resources.Load<Stats>($"DataSO/StatsData/Itemscrafteados/Stats_{itemID}");

        if (itemStats == null)
        {
            Debug.LogError($"Stats_{itemID} no encontrado en Resources.");
            return;
        }

        UnequipItem_Server(itemStats);
        DirtyStats = true;
    }

    // Applies stats of an equipped item
    public void EquipItem_Server(Stats itemStats)
    {
        if (itemStats == null) return;

        foreach (var itemStat in itemStats.statInfo)
        {
            var stat = localStats.FirstOrDefault(s => s.statType == itemStat.statType);

            if (stat != null)
                stat.statValue += itemStat.statValue;
            else
                localStats.Add(new StatInfo(itemStat.statType, itemStat.statValue));
        }

        OnStatsChanged?.Invoke();
        Debug.Log($"[SERVER] Item equipado: {itemStats.name}. Stats actualizadas.");
    }

    // Removes stats of an unequipped item
    public void UnequipItem_Server(Stats itemStats)
    {
        if (itemStats == null) return;

        foreach (var itemStat in itemStats.statInfo)
        {
            var stat = localStats.FirstOrDefault(s => s.statType == itemStat.statType);

            if (stat != null)
            {
                stat.statValue -= itemStat.statValue;

                if (stat.statValue < 0)
                    stat.statValue = 0;
            }
        }

        OnStatsChanged?.Invoke();
        Debug.Log($"[SERVER] Item desequipado: {itemStats.name}. Stats actualizadas.");
    }

    // Render called on client: fires event if stats are dirty
    public override void Render()
    {
        if (DirtyStats)
        {
            OnStatsChanged?.Invoke();
            DirtyStats = false;
            DebugStats("CLIENT");

        }
    }
    
    // Debug helper to print current stats
    public void DebugStats(string origin)
    {
        string s = $"[{origin}] {Object.InputAuthority} | Stats: ";

        foreach (var stat in localStats)
        {
            s += $"{stat.statType}={stat.statValue} ";
        }

        Debug.Log(s);
    }
    
    // Debug input: press P to print stats
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            DebugStats(Object.HasStateAuthority ? "SERVER" : "CLIENT");
        }
    }
}