using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// Handles mission lifecycle and progress by listening to gameplay events.
// Supports missions that can temporarily pause others (e.g., dungeon missions).

public class MissionController : MonoBehaviour
{
    [SerializeField] private MissionData playgroundMission;//starting mission
    [SerializeField] private List<MissionData> allMissions;

    // Active missions currently running in the game
    private List<MissionData> _currentMissions = new List<MissionData>();
    public IReadOnlyList<MissionData> CurrentMissions => _currentMissions;

    private bool missionsPaused = false;// When true, only the pausing mission can receive progress updates
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
        // Optional starting mission used mainly for playground/testing
        if (playgroundMission != null)
        {
            StartNewMission(playgroundMission);
        }
    }

    public void StartNewMission(MissionData missionData)
    {
        // Instantiate a runtime copy so progress is not stored in the asset
        var newMission = Instantiate(missionData);
        newMission.ResetProgress();
        _currentMissions.Add(newMission);

        // Some missions (like dungeons) temporarily pause all others
        if (newMission.pausesOtherMissions)
        {
            missionsPaused = true;
            pausingMission = newMission;
            Debug.Log("Mission started that pauses other missions");
        }

        Debug.Log($"Mission Started: {newMission.missionId} | Steps: {newMission.missionSteps.Count}");
        MissionEvents.OnMissionStarted?.Invoke(newMission);
    }

    public void TrackStep(GameEventType stepId, int progress)
    {
        // Start missions that are triggered by this event
        foreach (var mission in allMissions)
        {
            if (mission.startWithEvent && mission.startEvent == stepId)
            {
                bool alreadyRunning = _currentMissions.Any(m => m.missionId == mission.missionId);

                if (!alreadyRunning)
                    StartNewMission(mission);
            }
        }

        // If missions are paused, only the pausing mission can continue
        if (missionsPaused && pausingMission == null) return;
        if (_currentMissions.Count == 0) return;

        List<MissionData> missionsToRemove = new List<MissionData>();
        List<MissionData> missionsToStart = new List<MissionData>();

        foreach (var mission in _currentMissions.ToList())
        {
            // While paused, ignore all missions except the pausing one
            if (missionsPaused && mission != pausingMission)
                continue;

            if (!mission.UpdateProgress(stepId, progress, out var isSuccess))
            {
                MissionEvents.OnMissionProgress?.Invoke(mission);
                continue;
            }

            if (isSuccess)
            {
                Debug.Log($"Mission Completed: {mission.missionId}");
                MissionEvents.OnMissionComplete?.Invoke(mission);
                // When the pausing mission finishes successfully,
                // normal mission tracking resumes

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
                //AQUI algo como MissionEvents.OnMissionComplete?.Invoke(mission.id)
                missionsToRemove.Add(mission);
            }
            else
            {
                Debug.Log($"Mission Failed: {mission.missionId}");
                MissionEvents.OnMissionFailed?.Invoke(mission);
                // DESIGN DECISION:
                // If the pausing mission fails, other missions also resume.
                // The dungeon (or special mission) simply ends and the
                // rest of the mission system continues normally.
                if (mission == pausingMission)
                {
                    missionsPaused = false;
                    pausingMission = null;
                    Debug.Log("Dungeon failed → missions resumed");
                }

                missionsToRemove.Add(mission);
            }
        }

        // remover misiones
        foreach (var mission in missionsToRemove)
        {
            _currentMissions.Remove(mission);
            Destroy(mission);
        }
        MissionEvents.OnMissionListChanged?.Invoke();

        // iniciar nuevas misiones
        foreach (var next in missionsToStart)
        {
            StartNewMission(next);
        }
    }
}
