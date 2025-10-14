using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum Stat
{
    maxHealth,
    damage,
    speed,
    armor
}



[CreateAssetMenu(menuName = "Stats", fileName = "Stat")]
public class Stats : ScriptableObject
{
    public List<StatInfo> statInfo = new List<StatInfo>();

    public float GetStat(Stat stat)
    {
        foreach (var s in statInfo)
        {
            if (s.statType == stat)
            {
                return s.statValue;
            }
        }

        Debug.LogError("No stat value found for " + stat + " on " + name);
        return 0;
    }

}
