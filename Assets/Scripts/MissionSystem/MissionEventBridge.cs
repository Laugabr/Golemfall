using Fusion;
using UnityEngine;

// Va en el prefab del jugador.
// Escucha eventos locales y los reenvía al MissionController global.
public class MissionEventBridge : NetworkBehaviour
{
    private MissionController _missionController;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority) return;

        _missionController = FindFirstObjectByType<MissionController>();

        if (_missionController == null)
        {
            Debug.LogError("MissionEventBridge: MissionController no encontrado");
            return;
        }

        TrackEvents.OnTrackEvent += OnLocalEvent;

        // Si soy cliente (no host), pido el estado actual de las misiones
        if (!Runner.IsServer)
        {
            _missionController.RPC_RequestSync();
            Debug.Log("MissionEventBridge: sync pedido al servidor");
        }
    }

    private void OnDestroy()
    {
        TrackEvents.OnTrackEvent -= OnLocalEvent;
    }

    private void OnLocalEvent(GameEventType stepId, int progress)
    {
        if (_missionController == null) return;

        // El host ya procesa los eventos via ServerTrackStep, no necesita el RPC
        if (Runner.IsServer) return;

        // Mandar el evento al servidor para que procese la misión grupal
        _missionController.RPC_ServerReceiveEvent(stepId, progress);
    }

}