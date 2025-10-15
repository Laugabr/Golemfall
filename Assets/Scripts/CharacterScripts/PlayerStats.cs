using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : CharacterStats
{
    [Header("Events")]
    public UnityEvent OnStatsChanged; //Notifys when a stat is changed
    public void EquipItem(Stats itemStats)
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

        OnStatsChanged?.Invoke(); // On stats canged event is called 
        Debug.Log($"Se equipó item {itemStats.name}. Stats actualizadas.");
    }

    public void UnequipItem(Stats itemStats)
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

        OnStatsChanged?.Invoke(); // 
        Debug.Log($"Se quitó item {itemStats.name}. Stats actualizadas.");
        
    }

}


