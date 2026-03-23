using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;

// Handles mission lifecycle and progress by listening to gameplay events.
// Supports missions that can temporarily pause others (e.g., dungeon missions).

public class MissionController : NetworkBehaviour
{
    [SerializeField] private MissionData playgroundMission; // starting mission
    [SerializeField] private List<MissionData> allMissions;

    // Active missions currently running in the game
    private List<MissionData> _currentMissions = new();
    public IReadOnlyList<MissionData> CurrentMissions => _currentMissions;

    // When true only the pausing mission receives progress
    private bool missionsPaused = false;
    private MissionData pausingMission;

    private const string MISSION_PATH = "Missions/";
    private bool isTrackingEvents = false;

    #region Networking

    #region Server

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ServerStartMission(string missionId, RpcInfo info)
    {
        if (!Object.HasStateAuthority) return;

        if (string.IsNullOrEmpty(missionId))
        {
            RPC_HandlerError("Received null mission id");
            return;
        }

        var missionData = Resources.Load<MissionData>($"{MISSION_PATH}{missionId}");

        if (missionData == null)
        {
            RPC_HandlerError($"Mission {missionId} not found");
            return;
        }

        StartNewMission(missionData);

        // Start listening to gameplay events only once
        if (!isTrackingEvents)
        {
            TrackEvents.OnTrackEvent += ServerTrackStep;
            isTrackingEvents = true;
        }

        RPC_ClientStartMission(missionId);
    }

    #endregion

    #region Client

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_HandlerError(string error, RpcInfo info = default)
    {
        Debug.LogError(error);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ClientStartMission(string missionId, RpcInfo info = default)
    {
        if (!Object.HasInputAuthority) return;

        var missionData = Resources.Load<MissionData>($"{MISSION_PATH}{missionId}");

        if (missionData == null)
        {
            Debug.LogError($"Mission {missionId} not found");
            return;
        }

        StartNewMission(missionData);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ClientUpdateProgress(GameEventType id, int progress, RpcInfo info = default)
    {
        if (!Object.HasInputAuthority) return;

        ClientTrackStep(id, progress);
    }

    #endregion

    #endregion

    private void Start()
    {
        if (!Object.HasStateAuthority) return;

        if (playgroundMission != null)
        {
            StartNewMission(playgroundMission);

            if (!isTrackingEvents)
            {
                TrackEvents.OnTrackEvent += ServerTrackStep;
                isTrackingEvents = true;
            }
        }
    }

    private void OnDestroy()
    {
        if (isTrackingEvents)
        {
            TrackEvents.OnTrackEvent -= ServerTrackStep;
            isTrackingEvents = false;
        }
    }

    public void StartNewMission(MissionData missionData)
    {
        // Instantiate runtime copy so the asset is never modified
        var newMission = Instantiate(missionData);
        newMission.ResetProgress();

        _currentMissions.Add(newMission);

        // Some missions pause all other missions (ex: dungeon)
        if (newMission.pausesOtherMissions)
        {
            missionsPaused = true;
            pausingMission = newMission;
        }

        Debug.Log($"Mission Started: {newMission.missionId}");
        MissionEvents.OnMissionStarted?.Invoke(newMission);
    }

    private void ServerTrackStep(GameEventType stepId, int progress)
    {
        if (!Object.HasStateAuthority) return;

        var status = TrackStep(stepId, progress);

        // Only notify clients if something actually changed
        if (status != MissionStatus.kNone)
        {
            RPC_ClientUpdateProgress(stepId, progress);
        }
    }

    // SERVER LOGIC
    public MissionStatus TrackStep(GameEventType stepId, int progress)
    {
        if (!Object.HasStateAuthority)
            return MissionStatus.kNone;

        MissionStatus globalStatus = MissionStatus.kNone;

        // Missions that start from gameplay events
        foreach (var mission in allMissions)
        {
            if (mission.startWithEvent && mission.startEvent == stepId)
            {
                bool alreadyRunning = _currentMissions.Any(m => m.missionId == mission.missionId);

                if (!alreadyRunning)
                    StartNewMission(mission);
            }
        }

        // Safety check
        if (missionsPaused && pausingMission == null)
        {
            Debug.LogWarning("Paused missions but pausingMission is null");
            missionsPaused = false;
        }

        if (_currentMissions.Count == 0)
            return MissionStatus.kNone;

        List<MissionData> missionsToRemove = new();
        List<MissionData> missionsToStart = new();

        foreach (var mission in _currentMissions.ToList())
        {
            // Ignore all missions except the active pausing one
            if (missionsPaused && mission != pausingMission)
                continue;

            mission.UpdateProgress(stepId, progress, out var status);

            switch (status)
            {
                case MissionStatus.kNone:
                    break;

                case MissionStatus.kHasProgress:

                    MissionEvents.OnMissionProgress?.Invoke(mission);
                    globalStatus = MissionStatus.kHasProgress;
                    break;

                case MissionStatus.kComplete:

                    Debug.Log($"Mission Completed: {mission.missionId}");
                    MissionEvents.OnMissionComplete?.Invoke(mission);
                    globalStatus = MissionStatus.kComplete;

                    if (mission == pausingMission)
                    {
                        missionsPaused = false;
                        pausingMission = null;
                        Debug.Log("Dungeon finished → missions resumed");
                    }

                    foreach (var next in mission.nextMissions)
                        missionsToStart.Add(next);

                    missionsToRemove.Add(mission);
                    break;

                case MissionStatus.kFailed:

                    Debug.Log($"Mission Failed: {mission.missionId}");
                    MissionEvents.OnMissionFailed?.Invoke(mission);
                    globalStatus = MissionStatus.kFailed;

                    if (mission == pausingMission)
                    {
                        missionsPaused = false;
                        pausingMission = null;
                        Debug.Log("Dungeon failed → missions resumed");
                    }

                    missionsToRemove.Add(mission);
                    break;
            }
        }

        // Remove finished missions
        foreach (var mission in missionsToRemove)
        {
            _currentMissions.Remove(mission);
            Destroy(mission);
        }

        MissionEvents.OnMissionListChanged?.Invoke();

        // Start chained missions
        foreach (var next in missionsToStart)
        {
            StartNewMission(next);
        }

        return globalStatus;
    }

    // CLIENT ONLY: update mission UI
    private void ClientTrackStep(GameEventType stepId, int progress)
    {
        foreach (var mission in _currentMissions)
        {
            mission.UpdateProgress(stepId, progress, out var status);

            if (status == MissionStatus.kHasProgress)
            {
                MissionEvents.OnMissionProgress?.Invoke(mission);
            }
        }
    }
}