using System;
using UnityEngine;

// Serializable class representing a single stat type and its value
[Serializable]// Allows editing in the Unity Inspector
public class StatInfo
{
    public Stat statType;
    public int statValue;

    // Constructor to initialize the stat type and value
    public StatInfo(Stat statType, int statValue)
    {
        this.statType = statType;
        this.statValue = statValue;
    }
}