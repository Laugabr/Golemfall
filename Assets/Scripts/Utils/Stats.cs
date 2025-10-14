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
    public List<StatInfo> stats = new List<StatInfo>();

}
