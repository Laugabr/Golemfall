using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MissionData", menuName = "Scriptable Objects/MissionData")]
public class MissionData : ScriptableObject
{
    public string missionId; //ejemplo Mission_Forest_001
    public string missionName; //ej Find the special forest item
    public string description; //explorar el bosque y encontrar el item escondido
    public int xp;
    public int coins;
    public List<MissionStep> missionSteps;
    public List<MissionStep> failureSteps;
    public List<MissionData> nextMissions;
    public bool pausesOtherMissions;
    //public bool allowTeleportParty;
    //public Vector3 teleportDestination;
    //public Scene teleportDestiny;

    public bool UpdateProgress(MissionStepType id, int progress, out bool success)
    {
        success = false;
        var allComplete = true;
        foreach (var steps in missionSteps)
        {
            steps.UpdateProgress(id, progress);
            if (!steps.isComplete)
            {
                allComplete = false;
            }
        }

        bool allFailure = failureSteps.Count > 0;

        foreach (var steps in failureSteps)
        {
            steps.UpdateProgress(id, progress);
            if (!steps.isComplete)
            {
                allFailure = false;
            }
        }

        if (allFailure)
        {
            //Fallaste la misión
            success = false;
            return true;
        }
        if (allComplete)
        {
            //Completaste la misión
            success = true;
            return true;
        }
        return false;
    }
    public void ResetProgress()
    {
        foreach (var step in missionSteps)
        {
            step.isComplete = false;
            step.currentAmount = 0;
        }

        foreach (var step in failureSteps)
        {
            step.isComplete = false;
            step.currentAmount = 0;
        }
    }
}
