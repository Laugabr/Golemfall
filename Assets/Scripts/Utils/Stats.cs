using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// Defines possible stat types
public enum Stat
{
    maxHealth,
    damage,
    speed,
    armor
}

// ScriptableObject to hold a collection of stats for items
[CreateAssetMenu(menuName = "Game Data/Stats", fileName = "NewStats")]
public class Stats : ScriptableObject
{
    [Header("Stat values for this object")]
    public List<StatInfo> statInfo = new List<StatInfo>();// List of stats and their values
}