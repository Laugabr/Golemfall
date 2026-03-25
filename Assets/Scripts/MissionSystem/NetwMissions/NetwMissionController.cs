using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;

// Networked mission controller (server authoritative, no ScriptableObject runtime state)
public class NetwMissionController : NetworkBehaviour
{
    [SerializeField] private MissionData playgroundMission; // misión inicial
    [SerializeField] private List<MissionData> allMissions; // lista de todas las misiones posibles (para triggers)
    [SerializeField] private MissionDatabase missionDatabase; // base de datos que convierte entre ID <-> MissionData

    // Lista sincronizada en red de misiones activas
    [Networked, Capacity(10)]
    public NetworkLinkedList<NetworkMission> Missions => default;

    // Se ejecuta cuando el objeto de red spawnea
    public override void Spawned()
    {
        if (!Object.HasStateAuthority) return; // solo el server controla

        if (playgroundMission != null)
        {
            StartNewMission(playgroundMission); // iniciar misión inicial
        }
    }

    // Agrega una nueva misión a la lista networked
    public void StartNewMission(MissionData missionData)
    {
        if (!Object.HasStateAuthority) return; // solo server

        short id = missionDatabase.GetId(missionData); // obtener ID de la misión

        // evitar duplicados
        foreach (var m in Missions)
        {
            if (m.missionId == id)
                return;
        }

        // crear estructura networked
        NetworkMission netMission = new NetworkMission
        {
            missionId = id,
            stepIndex = 0,          // paso actual
            currentAmount = 0,      // progreso actual del paso
            isComplete = false,
            isFailed = false
        };

        Missions.Add(netMission); // agregar a lista sincronizada

        Debug.Log($"[NET] Mission Started: {missionData.name}");
    }

    // Entrada desde eventos del juego (solo server)
    private void ServerTrackStep(GameEventType stepId, int progress)
    {
        if (!Object.HasStateAuthority) return;

        TrackStep(stepId, progress);
    }

    // Lógica principal de progreso de misiones
    public MissionStatus TrackStep(GameEventType stepId, int progress)
    {
        if (!Object.HasStateAuthority)
            return MissionStatus.kNone;

        MissionStatus globalStatus = MissionStatus.kNone;

        // Revisar si algún evento inicia nuevas misiones
        foreach (var mission in allMissions)
        {
            if (mission.startWithEvent && mission.startEvent == stepId)
            {
                short id = missionDatabase.GetId(mission);

                bool alreadyRunning = false;

                // verificar si ya está activa
                foreach (var m in Missions)
                {
                    if (m.missionId == id)
                    {
                        alreadyRunning = true;
                        break;
                    }
                }

                // iniciar si no existe
                if (!alreadyRunning)
                    StartNewMission(mission);
            }
        }

        // Iterar misiones activas
        for (int i = 0; i < Missions.Count; i++)
        {
            var netMission = Missions[i];

            // ignorar misiones terminadas
            if (netMission.isComplete || netMission.isFailed)
                continue;

            // obtener datos estáticos de la misión
            var missionData = missionDatabase.GetMission(netMission.missionId);
            if (missionData == null) continue;

            // validar índice de paso
            if (netMission.stepIndex >= missionData.missionSteps.Count)
                continue;

            var step = missionData.missionSteps[netMission.stepIndex];

            // verificar si el evento corresponde al paso actual
            if (step.targetId != stepId)
                continue;

            // sumar progreso
            netMission.currentAmount += (short)progress;

            // verificar si se completa el paso
            if (netMission.currentAmount >= step.amount)
            {
                netMission.stepIndex++;       // avanzar al siguiente paso
                netMission.currentAmount = 0; // resetear progreso

                // verificar si se completó toda la misión
                if (netMission.stepIndex >= missionData.missionSteps.Count)
                {
                    netMission.isComplete = true;
                    globalStatus = MissionStatus.kComplete;

                    Debug.Log($"[NET] Mission Complete: {missionData.name}");
                }
                else
                {
                    globalStatus = MissionStatus.kHasProgress;
                }
            }
            else
            {
                globalStatus = MissionStatus.kHasProgress;
            }

            // guardar cambios en la lista networked
            Missions.Set(i, netMission);
        }

        return globalStatus;
    }
}