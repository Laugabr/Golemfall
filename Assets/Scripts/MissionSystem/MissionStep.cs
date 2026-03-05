using UnityEngine;
using System;

[System.Serializable]
public class MissionStep
{
    public MissionStepType targetId;
    public int amount; //cuanta cantidad de enemigos por ej para completar la misión
    [NonSerialized] public bool isComplete;
    [NonSerialized] public int currentAmount; //nonserialized es para que el game designer no lo toque y no lo vea en el inspector

    public void UpdateProgress(MissionStepType id, int progress)
    {
        if (isComplete) return;
        if (id != targetId) return;

        currentAmount += progress;

        Debug.Log($"Step [{targetId}] progress: {currentAmount}/{amount}");

        if (currentAmount >= amount)
        {
            isComplete = true;
            Debug.Log($"Step [{targetId}] COMPLETED");
        }
    }

}
