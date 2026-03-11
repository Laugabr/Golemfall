using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using System.Diagnostics;

// Handles mission lifecycle and progress by listening to gameplay events.
// Supports missions that can temporarily pause others (e.g., dungeon missions).

public class MissionController : NetworkBehavior
{
    [SerializeField] private MissionData playgroundMission;//starting mission
    [SerializeField] private List<MissionData> allMissions;

    // Active missions currently running in the game
    private List<MissionData> _currentMissions = new List<MissionData>();
    public IReadOnlyList<MissionData> CurrentMissions => _currentMissions;

    private bool missionsPaused = false;// When true, only the pausing mission can receive progress updates
    private MissionData pausingMission;

    private const string MISSION_PATH = "Missions/";//si por alguna razon queremos cambiar 
                                                    // la ruta de acceso, lo mejor es tenerlo siempre en una constante
    private bool isTrackingEvents = false;

    #region Networking

    #region Server

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ServerStartMission(string missionId, RpcInfo info)
    {
        if (!Object.HasStateAuthority) return;

        if (string.IsNullOrEmpty(missionId))
        {
            RPC_HandlerError("Se recibió un id nulo al intentar iniciar una misión");
            return;
        }

        var missionData = Resources.Load<MissionData>($"{MISSION_PATH}{missionId}");

        if (missionData == null)
        {
            RPC_HandlerError($"No se encontró la misión {missionId}");
            return;
        }

        StartNewMission(missionData);

        if (!isTrackingEvents)
        {
            TrackEvents.OnTrackEvent += TrackStep;
            isTrackingEvents = true;
        }

        RPC_ClientStartMission(missionId);
    }

    #endregion

    #region Client

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_HandlerError(string error, RpcInfo info = default)
    {
        //Implementar manejo de errores
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ClientStartMission(string missionId, RpcInfo info = default)
    {
        if (!Object.HasInputAuthority) return;

        if (string.IsNullOrEmpty(missionId))
        {
            Debug.LogError("Se recibió un id nulo al intentar iniciar una misión");
            return;
        }

        var missionData = Resources.Load<MissionData>($"{MISSION_PATH}{missionId}");

        if (missionData == null)
        {
            Debug.LogError($"No se encontró la misión {missionId}");
            return;
        }

        StartNewMission(missionData);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ClientUpdateProgress(string id, int progress, RpcInfo info = default)
    {
        TrackStep(id, progress);
    }
    #endregion

    #endregion

    public void Start()
    {
        if (!Object.HasStateAuthority) return;

        if (playgroundMission != null)
        {
            StartNewMission(playgroundMission);

            if (!isTrackingEvents)
            {
                TrackEvents.OnTrackEvent += TrackStep;
                isTrackingEvents = true;
            }
        }
    }

    private void OnDestroy()
    {
        if (isTrackingEvents)
        {
            TrackEvents.OnTrackEvent -= TrackStep;
            isTrackingEvents = false;
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

    private void ServerTrackStep(GameEventType stepId, int progress)
    {
        if (!Object.HasStateAuthority) return;

        var status = TrackStep(stepId, progress);


        //RPC_ClientUpdateProgress(stepId, progress)
        switch (status)
        {
            case MissionStatus.kFailed:
                //RPC_FailedCurrentMission();
                break;
            case MissionStatus.kComplete:
                //RPC_CompleteCurrentMission();
                break;
            case MissionStatus.kHasProgress:
                RPC_ClientUpdateProgress(stepId, progress);
                break;
            default:
        }

    }

    public MissionStatus TrackStep(GameEventType stepId, int progress)
    {
        if (!Object.HasStateAuthority) return;

        //Aca el profe hace para una sola mision, nuestro diseño es lista de misiones
        if (_currentMission == null) return MissionStatus.kNone;

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


        //Aqui el profe puso para una mision, habria que adaptarlo a nuestro sistema
        // que está hecho para soportar misiones simultaneas
        //Ademas borro varias lineas anteriores
        _currentMission.UpdateProgress(stepId, progress, out var status);

        switch (status)
        {
            case MissionStatus.kFailed:
                FailureMission();
                break;
            case MissionStatus.kComplete:
                CompleteMission();
                break;
            case MissionStatus.kHasProgress:
                //Actualizar HUD
                MissionEvents.OnUpdateProgress?.Invoke(_currentMission);
                break;
            default:
        }
        return status;
    }
}
