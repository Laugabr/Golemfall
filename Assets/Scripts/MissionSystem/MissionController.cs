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

    // IDs de misiones que ya terminaron (completadas o fallidas no se trackean igual,
    // solo guardamos completadas para DialogueRequirement.MissionComplete)
    private HashSet<string> _completedMissionIds = new();
    public bool IsMissionComplete(string missionId) => _completedMissionIds.Contains(missionId);

    // When true only the pausing mission receives progress
    private bool missionsPaused = false;
    private MissionData pausingMission;

    private const string MISSION_PATH = "DataSO/Missions/";
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
        RPC_AllClientsStartMission(missionId);

        if (!isTrackingEvents)
        {
            TrackEvents.OnTrackEvent += ServerTrackStep;
            isTrackingEvents = true;
        }
    }

    #endregion

    #region Client

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_HandlerError(string error, RpcInfo info = default)
    {
        Debug.LogError(error);
    }

    // Llega a TODOS los clientes (no solo al dueño)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AllClientsStartMission(string missionId, RpcInfo info = default)
    {
        // El servidor ya la inició arriba, no la repite
        if (Object.HasStateAuthority) return;

        var missionData = Resources.Load<MissionData>($"{MISSION_PATH}{missionId}");
        if (missionData == null)
        {
            Debug.LogError($"Mission {missionId} not found on client");
            return;
        }

        StartNewMission(missionData);
    }

    // El servidor avisa a TODOS que hubo progreso
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AllClientsUpdateProgress(GameEventType id, int progress, RpcInfo info = default)
    {
        // El servidor ya procesó la lógica en TrackStep, solo actualiza UI
        if (Object.HasStateAuthority) return;

        ClientTrackStep(id, progress);
    }

    // Cualquier cliente puede mandar un evento de juego al servidor
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ServerReceiveEvent(GameEventType stepId, int progress, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;

        var status = TrackStep(stepId, progress);

        if (status != MissionStatus.kNone)
        {
            RPC_AllClientsUpdateProgress(stepId, progress);
        }
    }

    // El cliente recién unido pide sincronizarse con el estado actual
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSync(RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;

        // Mandar todas las misiones activas al cliente que pidió sync

        foreach (var mission in _currentMissions)
        {
            RPC_AllClientsStartMission(mission.missionId);

            // Mandar el progreso de cada step
            for (int i = 0; i < mission.missionSteps.Count; i++)
            {
                var step = mission.missionSteps[i];
                if (step.currentAmount > 0)
                {
                    RPC_SyncMissionProgress(mission.missionId, i, step.currentAmount);
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SyncMissionProgress(string missionId, int stepIndex, int currentAmount, RpcInfo info = default)
    {
        if (Object.HasStateAuthority) return;

        var mission = _currentMissions.FirstOrDefault(m => m.missionId == missionId);
        if (mission == null) return;
        if (stepIndex < 0 || stepIndex >= mission.missionSteps.Count) return;

        mission.missionSteps[stepIndex].currentAmount = currentAmount;
        mission.missionSteps[stepIndex].isComplete = currentAmount >= mission.missionSteps[stepIndex].amount;

        MissionEvents.OnMissionProgress?.Invoke(mission);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AllClientsRemoveMission(string missionId, RpcInfo info = default)
    {
        if (Object.HasStateAuthority) return;

        var mission = _currentMissions.FirstOrDefault(m => m.missionId == missionId);
        if (mission == null) return;

        _currentMissions.Remove(mission);
        Destroy(mission);
        MissionEvents.OnMissionListChanged?.Invoke();
    }
    #endregion

    #endregion

    public override void Spawned()
    {
        if (!Object.HasStateAuthority) return;

        if (playgroundMission != null)
        {
            StartNewMission(playgroundMission);
            RPC_AllClientsStartMission(playgroundMission.missionId);
        }

        if (!isTrackingEvents)
        {
            TrackEvents.OnTrackEvent += ServerTrackStep;
            isTrackingEvents = true;
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

        // Solo sincronizar progreso parcial, no el evento que completa la misión
        if (status == MissionStatus.kHasProgress)
        {
            RPC_AllClientsUpdateProgress(stepId, progress);
        }
        // kComplete y kFailed no necesitan sync aquí
        // porque RPC_AllClientsRemoveMission y RPC_AllClientsStartMission ya lo manejan
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
                {
                    StartNewMission(mission);
                    RPC_AllClientsStartMission(mission.missionId);
                }
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
                    _completedMissionIds.Add(mission.missionId);
                    MissionEvents.OnMissionComplete?.Invoke(mission);
                    globalStatus = MissionStatus.kComplete;
                    RPC_AllClientsRemoveMission(mission.missionId);

                    if (mission.xp > 0)
                    {
                        var allExpManagers = FindObjectsByType<ExperienceManager>(FindObjectsSortMode.None);
                        foreach (var em in allExpManagers)
                        {
                            em.AddExperience(mission.xp);
                        }
                        Debug.Log($"[MISSION] XP grupal otorgada: {mission.xp} a {allExpManagers.Length} players");
                    }
                    
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
                    RPC_AllClientsRemoveMission(mission.missionId);

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
            RPC_AllClientsStartMission(next.missionId);
        }

        return globalStatus;
    }

    // CLIENT ONLY: update mission UI
    private void ClientTrackStep(GameEventType stepId, int progress)
    {
        foreach (var mission in _currentMissions.ToList())
        {
            // Solo procesar misiones que ya existían antes de este evento
            // ignorar misiones que acaban de empezar en este mismo tick
            mission.UpdateProgress(stepId, progress, out var status);

            switch (status)
            {
                case MissionStatus.kHasProgress:
                    MissionEvents.OnMissionProgress?.Invoke(mission);
                    break;
                case MissionStatus.kComplete:
                    MissionEvents.OnMissionComplete?.Invoke(mission);
                    break;
                case MissionStatus.kFailed:
                    MissionEvents.OnMissionFailed?.Invoke(mission);
                    break;
            }
        }
    }
}