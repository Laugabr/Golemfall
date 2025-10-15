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

[CreateAssetMenu(menuName = "Game Data/Stats", fileName = "NewStats")]
public class Stats : ScriptableObject
{
    [Header("Stat values for this object")]
    public List<StatInfo> statInfo = new List<StatInfo>();
}