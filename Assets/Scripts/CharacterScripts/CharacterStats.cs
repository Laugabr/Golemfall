using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

public class CharacterStats : NetworkBehaviour
{
    [Header("Base Stats")]
    public Stats baseStats;

    [Header("Runtime Stats")]
    public List<StatInfo> localStats = new List<StatInfo>();
    public virtual void Initialize()
    {
        // Copiamos los valores base
        localStats = new List<StatInfo>(
            baseStats.statInfo.Select(s => new StatInfo(s.statType, s.statValue))
        );
    }
    void Start()
    {
        Initialize();
    }
    public int GetStat(Stat stat)
    {
        return localStats.FirstOrDefault(s => s.statType == stat)?.statValue ?? 0;
    }
    public void LogCurrentStats()
    {
        Debug.Log("=== Estado actual de las stats del jugador ===");
        foreach (var stat in localStats)
        {
            Debug.Log($"• {stat.statType}: {stat.statValue}");
        }
    }
}