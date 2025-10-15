using System;
using UnityEngine;

[Serializable] //Para poder asiganrle los valores en el inspector
public class StatInfo
{
    public Stat statType;
    public int statValue;

    public StatInfo(Stat statType, int statValue)
    {
        this.statType = statType;
        this.statValue = statValue;
    }
}