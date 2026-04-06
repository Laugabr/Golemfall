using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

/*
  CharacterStats
 
  Holds and manages character statistics such as speed, health, etc.
  Initializes runtime stats from base stats and provides access methods.
  Supports logging for debugging purposes.
 */

public class CharacterStats : NetworkBehaviour
{
    [Header("Base Stats")]
    public Stats baseStats; // Scriptable base stats configuration

    [Header("Runtime Stats")]
    public List<StatInfo> localStats = new List<StatInfo>(); // Current in-game stats
    public virtual void Initialize()
    {
        // Copy base stats into runtime list to allow runtime modifications
        localStats = new List<StatInfo>(
            baseStats.statInfo.Select(s => new StatInfo(s.statType, s.statValue))
        );
    }


    void Start()
    {
        // Initialize stats when the character spawns

        Initialize();
    }
    public int GetStat(Stat stat)
    {
        // Returns the value of the requested stat, defaulting to 0 if missing

        return localStats.FirstOrDefault(s => s.statType == stat)?.statValue ?? 0;
    }
    public void LogCurrentStats()
    {
        // Debug output showing all current runtime stats

        Debug.Log("=== Estado actual de las stats del jugador ===");
        foreach (var stat in localStats)
        {
            Debug.Log($"• {stat.statType}: {stat.statValue}");
        }
    }
}