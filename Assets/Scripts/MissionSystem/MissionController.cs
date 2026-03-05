using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;


public class MissionController : MonoBehaviour
{
    [SerializeField] private MissionData playgroundMission;
    [SerializeField] private MissionData caveMission;

    private List<MissionData> _currentMissions = new List<MissionData>();
    public IReadOnlyList<MissionData> CurrentMissions => _currentMissions;

    private bool missionsPaused = false;
    private MissionData pausingMission;

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

        if (newMission.pausesOtherMissions)
        {
            missionsPaused = true;
            pausingMission = newMission;
            Debug.Log("Mission started that pauses other missions");
        }

        Debug.Log($"Mission Started: {newMission.missionId} | Steps: {newMission.missionSteps.Count}");
    }

    public void TrackStep(MissionStepType stepId, int progress)
    {
        if (stepId == MissionStepType.EnterCave)
        {
            if (caveMission != null)
                StartNewMission(caveMission);
        }

        if (missionsPaused && pausingMission == null) return;
        if (_currentMissions.Count == 0) return;

        List<MissionData> missionsToRemove = new List<MissionData>();
        List<MissionData> missionsToStart = new List<MissionData>();

        foreach (var mission in _currentMissions.ToList())
        {
            if (missionsPaused && mission != pausingMission)
                continue;

            if (!mission.UpdateProgress(stepId, progress, out var isSuccess))
                continue;

            if (isSuccess)
            {
                Debug.Log($"Mission Completed: {mission.missionId}");

                if (mission == pausingMission)
                {
                    missionsPaused = false;
                    pausingMission = null;
                    Debug.Log("Dungeon finished → missions resumed");

                }

                foreach (var next in mission.nextMissions)
                {
                    missionsToStart.Add(next);
                }

                missionsToRemove.Add(mission);
            }
            else
            {
                Debug.Log($"Mission Failed: {mission.missionId}");
                missionsToRemove.Add(mission);
            }
        }

        // remover misiones
        foreach (var mission in missionsToRemove)
        {
            _currentMissions.Remove(mission);
            Destroy(mission);
        }

        // iniciar nuevas misiones
        foreach (var next in missionsToStart)
        {
            StartNewMission(next);
        }
    }
}
