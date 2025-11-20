using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

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

    //
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

           
    public override void Render()
    {
        if (DirtyStats)
        {
            OnStatsChanged?.Invoke();
            DirtyStats = false;
            DebugStats("CLIENT");

        }
    }
    public void DebugStats(string origin)
    {
        string s = $"[{origin}] {Object.InputAuthority} | Stats: ";

        foreach (var stat in localStats)
        {
            s += $"{stat.statType}={stat.statValue} ";
        }

        Debug.Log(s);
    }
    void Update()
{
    if (Input.GetKeyDown(KeyCode.P))
    {
        DebugStats(Object.HasStateAuthority ? "SERVER" : "CLIENT");
    }
}
}