using UnityEngine;
using System;

[System.Serializable]
public class MissionStep
{
    public GameEventType targetId;
    public int amount;

    [Tooltip("Key opcional para objetivos específicos (ej: 'z1_redtree'). " +
             "Vacío = cuenta cualquier evento de este tipo (wildcard).")]
    public string targetKey;

    [Tooltip("Texto para mostrar en el panel. Si está vacío, usa targetId.")]
    public string displayText;

    [NonSerialized] public bool isComplete;
    [NonSerialized] public int currentAmount;

    public string DisplayLabel => string.IsNullOrEmpty(displayText) ? targetId.ToString() : displayText;

    public bool TryUpdateProgress(GameEventType id, int progress, string key)
    {
        if (isComplete) return false;
        if (id != targetId) return false;

        // Matching del key:
        //  - targetKey vacío     -> wildcard: cuenta cualquier evento de este tipo (con o sin key).
        //  - targetKey con valor -> match exacto: solo cuenta si el key del evento coincide.
        if (!string.IsNullOrEmpty(targetKey) && targetKey != key)
            return false;

        currentAmount += progress;

        string label = string.IsNullOrEmpty(targetKey) ? targetId.ToString() : $"{targetId}:{targetKey}";
        Debug.Log($"Step [{label}] progress: {currentAmount}/{amount}");

        if (currentAmount >= amount)
        {
            isComplete = true;
            Debug.Log($"Step [{label}] COMPLETED");
        }
        return true;
    }
}