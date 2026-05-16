using UnityEngine;
using System;

[System.Serializable]
public class MissionStep
{
    public GameEventType targetId;
    public int amount;

    [Tooltip("Texto para mostrar en el panel. Si está vacío, usa targetId.")]
    public string displayText;

    [NonSerialized] public bool isComplete;
    [NonSerialized] public int currentAmount;

    public string DisplayLabel => string.IsNullOrEmpty(displayText) ? targetId.ToString() : displayText;

    public bool TryUpdateProgress(GameEventType id, int progress)
    {
        if (isComplete) return false;
        if (id != targetId) return false;

        currentAmount += progress;

        Debug.Log($"Step [{targetId}] progress: {currentAmount}/{amount}");

        if (currentAmount >= amount)
        {
            isComplete = true;
            Debug.Log($"Step [{targetId}] COMPLETED");
        }
        return true;
    }
}