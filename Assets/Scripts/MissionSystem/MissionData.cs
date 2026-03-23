using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MissionData", menuName = "Scriptable Objects/MissionData")]
public class MissionData : ScriptableObject
{
    public string missionId;
    public string missionName;
    public string description;
    public int xp;
    public int coins;
    public List<MissionStep> missionSteps;
    public List<MissionStep> failureSteps;
    public List<MissionData> nextMissions;
    public bool pausesOtherMissions;
    public bool startWithEvent;
    public GameEventType startEvent;
    //public bool allowTeleportParty;
    //public Vector3 teleportDestination;
    //public Scene teleportDestiny;

    public void UpdateProgress(GameEventType id, int progress, out MissionStatus status)
    {
        status = MissionStatus.kNone;
        var allComplete = true;
        foreach (var steps in missionSteps)
        {
            if(steps.TryUpdateProgress(id, progress))
            {
                status = MissionStatus.kHasProgress;
            }

            if (!steps.isComplete)
            {
                allComplete = false;
            }
        }

        bool allFailure = failureSteps.Count > 0;

        foreach (var steps in failureSteps)
        {
             if(steps.TryUpdateProgress(id, progress))
            {
                status = MissionStatus.kHasProgress;
            }

            if (!steps.isComplete)
            {
                allFailure = false;
            }
        }

        if (allFailure)
        {
            status = MissionStatus.kFailed;
            return;
        }
        if (allComplete)
        {
            status = MissionStatus.kComplete;
            return;
        }
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

public enum MissionStatus
{
    kNone,
    kHasProgress,
    kComplete,
    kFailed
}
