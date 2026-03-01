using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using System.Net;


public class MissionController : MonoBehaviour
{
    [SerializeField] private MissionData playgroundMission;

    private List<MissionData> _currentMissions = new List<MissionData>();
    public IReadOnlyList<MissionData> CurrentMissions => _currentMissions;

    private void OnEnable()
    {
        TrackEvents.OnTrackEvent += TrackStep;
    }

    private void OnDisable()
    {
        TrackEvents.OnTrackEvent -= TrackStep;
    }

    private void Start()
    {
        if (playgroundMission != null)
        {
            StartNewMission(playgroundMission);
        }
    }

    public void StartNewMission(MissionData missionData)
    {
        var newMission = Instantiate(missionData);
        newMission.ResetProgress();
        _currentMissions.Add(newMission);

        Debug.Log($"Mission Started: {newMission.missionId}");
    }

    public void TrackStep(string stepId, int progress)
    {
        if (_currentMissions.Count == 0) return;

        //Debug.Log($"TrackStep received: {stepId} | {progress}");

        List<MissionData> missionsToRemove = new List<MissionData>();

        foreach (var mission in _currentMissions)
        {
            if (!mission.UpdateProgress(stepId, progress, out var isSuccess))
                continue;

            if (isSuccess)
            {
                Debug.Log($"Mission Completed: {mission.missionId}");
                missionsToRemove.Add(mission);
            }
            else
            {
                Debug.Log($"Mission Failed: {mission.missionId}");
                missionsToRemove.Add(mission);
            }
        }

        foreach (var mission in missionsToRemove)
        {
            _currentMissions.Remove(mission);
            Destroy(mission);
        }
    }
}
