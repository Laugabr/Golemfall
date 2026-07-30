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

    // XP grupal total ya repartida por misiones completadas.
    // La consultan los jugadores que se unen tarde para ponerse al día.
    private int _groupXpAwarded;
    public int GroupXpAwarded => _groupXpAwarded;

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

        // Evita duplicados: este RPC puede llegar dos veces al mismo cliente
        // (broadcast inicial de Spawned + reenvío de RPC_RequestSync).
        if (_currentMissions.Any(m => m.missionId == missionId)) return;

        var missionData = Resources.Load<MissionData>($"{MISSION_PATH}{missionId}");
        if (missionData == null)
        {
            Debug.LogError($"Mission {missionId} not found on client");
            return;
        }

        StartNewMission(missionData);
    }

    // Cualquier cliente puede mandar un evento de juego al servidor
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ServerReceiveEvent(GameEventType stepId, int progress, string key, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;

        // El estado absoluto se sincroniza dentro de TrackStep (kHasProgress).
        TrackStep(stepId, progress, key);
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
    private void RPC_AllClientsRemoveMission(string missionId, bool completed, RpcInfo info = default)
    {
        if (Object.HasStateAuthority) return;

        var mission = _currentMissions.FirstOrDefault(m => m.missionId == missionId);
        if (mission == null) return;

        // El cliente ya no detecta complete/failed por su cuenta (lo decide el server),
        // así que el evento se dispara acá para que UI y notificaciones reaccionen.
        if (completed)
            MissionEvents.OnMissionComplete?.Invoke(mission);
        else
            MissionEvents.OnMissionFailed?.Invoke(mission);

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

        // Gatillos de activación hardcodeados
        BasicEventsManager.OnLevelUp += OnLevelUp;                       // nivel 5 → habilidad01
        BasicEventsManager.OnInventoryCountChanged += OnInventoryCount;  // 2 items → craft01
    }

    private void OnDestroy()
    {
        if (isTrackingEvents)
        {
            TrackEvents.OnTrackEvent -= ServerTrackStep;
            isTrackingEvents = false;
        }

        BasicEventsManager.OnLevelUp -= OnLevelUp;
        BasicEventsManager.OnInventoryCountChanged -= OnInventoryCount;
    }

    // Activación por nivel. Por ahora solo Habilidad01 al llegar a nivel 5.
    // Si en el futuro hay más misiones por nivel, conviene moverlo a data (MissionData).
    private void OnLevelUp(int level)
    {
        if (!Object.HasStateAuthority) return;
        if (level != 5) return;

        StartMissionById("mision_habilidad_01");
    }

    // Activación por inventario. Craft01 arranca cuando algún jugador junta 2 items.
    private void OnInventoryCount(int count)
    {
        if (!Object.HasStateAuthority) return;
        if (count < 2) return;

        StartMissionById("mision_craft_01");
    }

    // Arranque imperativo por id, con guard de duplicado / one-shot. Solo host.
    // Público para que lo puedan llamar triggers de escena (ej: MissionTriggerVolume).
    public void StartMissionById(string missionId)
    {
        if (!Object.HasStateAuthority) return;

        bool running = _currentMissions.Any(m => m.missionId == missionId);
        bool done = _completedMissionIds.Contains(missionId);
        if (running || done) return;

        var mission = Resources.Load<MissionData>($"{MISSION_PATH}{missionId}");
        if (mission == null)
        {
            Debug.LogError($"Mission {missionId} not found");
            return;
        }

        StartNewMission(mission);
        RPC_AllClientsStartMission(missionId);
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

        //Debug.Log($"Mission Started: {newMission.missionId}");
        MissionEvents.OnMissionStarted?.Invoke(newMission);
    }

    private void ServerTrackStep(GameEventType stepId, int progress, string key)
    {
        if (!Object.HasStateAuthority) return;
        // El sync (progreso absoluto y remove) se maneja dentro de TrackStep.
        TrackStep(stepId, progress, key);
    }

    // SERVER LOGIC
    public MissionStatus TrackStep(GameEventType stepId, int progress, string key)
    {
        if (!Object.HasStateAuthority)
            return MissionStatus.kNone;
        MissionStatus globalStatus = MissionStatus.kNone;

        // Missions that start from gameplay events
        foreach (var mission in allMissions)
        {
            if (mission.startWithEvent && mission.startEvent == stepId)
            {
                // Si la misión define un startEventKey, solo arranca cuando el key coincide.
                // Vacío = arranca con cualquier key (comodín).
                if (!string.IsNullOrEmpty(mission.startEventKey) && mission.startEventKey != key)
                    continue;

                bool alreadyRunning = _currentMissions.Any(m => m.missionId == mission.missionId);
                bool blockedAsOneShot = mission.startOnlyOnce && _completedMissionIds.Contains(mission.missionId);

                if (!alreadyRunning && !blockedAsOneShot)
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

            mission.UpdateProgress(stepId, progress, key, out var status);

            switch (status)
            {
                case MissionStatus.kNone:
                    break;

                case MissionStatus.kHasProgress:
                    MissionEvents.OnMissionProgress?.Invoke(mission);
                    SyncMissionStepsToClients(mission);
                    globalStatus = MissionStatus.kHasProgress;
                    break;

                case MissionStatus.kComplete:
                    //Debug.Log($"Mission Completed: {mission.missionId}");
                    _completedMissionIds.Add(mission.missionId);
                    MissionEvents.OnMissionComplete?.Invoke(mission);
                    globalStatus = MissionStatus.kComplete;
                    RPC_AllClientsRemoveMission(mission.missionId, true);

                    if (mission.xp > 0)
                    {
                        _groupXpAwarded += mission.xp;   // acumula para late joiners

                        var allExpManagers = FindObjectsByType<ExperienceManager>(FindObjectsSortMode.None);
                        foreach (var em in allExpManagers)
                        {
                            em.AddExperience(mission.xp);
                        }
                        //Debug.Log($"[MISSION] XP grupal otorgada: {mission.xp} a {allExpManagers.Length} players");
                    }

                    if (mission == pausingMission)
                    {
                        missionsPaused = false;
                        pausingMission = null;
                        //Debug.Log("Dungeon finished → missions resumed");
                    }

                    foreach (var next in mission.nextMissions)
                        missionsToStart.Add(next);

                    missionsToRemove.Add(mission);
                    break;

                case MissionStatus.kFailed:
                    //Debug.Log($"Mission Failed: {mission.missionId}");
                    MissionEvents.OnMissionFailed?.Invoke(mission);
                    globalStatus = MissionStatus.kFailed;
                    RPC_AllClientsRemoveMission(mission.missionId, false);

                    if (mission == pausingMission)
                    {
                        missionsPaused = false;
                        pausingMission = null;
                        //Debug.Log("Dungeon failed → missions resumed");
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

    // SERVER: envía el estado absoluto de los steps de una misión a todos los clientes.
    // Reusa el mismo RPC que RPC_RequestSync en vez de mandar deltas incrementales,
    // así el cliente nunca "suma" por su cuenta y no se puede desfasar el conteo.
    private void SyncMissionStepsToClients(MissionData mission)
    {
        for (int i = 0; i < mission.missionSteps.Count; i++)
        {
            RPC_SyncMissionProgress(mission.missionId, i, mission.missionSteps[i].currentAmount);
        }
    }
}