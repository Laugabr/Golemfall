using Fusion;
using UnityEngine;

// Lives on the player prefab.
// Listens to local gameplay events and forwards them to the global MissionController.
public class MissionEventBridge : NetworkBehaviour
{
    private MissionController _missionController;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority) return;

        _missionController = FindFirstObjectByType<MissionController>();

        if (_missionController == null)
        {
            Debug.LogError("MissionEventBridge: MissionController not found");
            return;
        }

        TrackEvents.OnTrackEvent += OnLocalEvent;

        // If we are a client (not host), request current mission state from server
        if (!Runner.IsServer)
        {
            _missionController.RPC_RequestSync();
            Debug.Log("MissionEventBridge: sync requested from server");
        }
    }

    private void OnDestroy()
    {
        TrackEvents.OnTrackEvent -= OnLocalEvent;
    }

    private void OnLocalEvent(GameEventType stepId, int progress, string key)
    {
        if (_missionController == null) return;

        // Host already processes events via ServerTrackStep, no RPC needed
        if (Runner.IsServer) return;

        // Send event to server so it can process group mission progress
        _missionController.RPC_ServerReceiveEvent(stepId, progress, key);

        // Individual XP: send to server for this player only
        int xp = GetXpForEvent(stepId) * progress;
        if (xp > 0)
        {
            var expManager = GetComponent<ExperienceManager>();
            expManager?.RPC_ServerAddExperience(xp);
        }
    }

    // Returns the XP reward for a given gameplay event type
    private int GetXpForEvent(GameEventType eventType)
    {
        return eventType switch
        {
            GameEventType.KillEnemy          => 20,
            GameEventType.BreakBreakable     => 3,
            GameEventType.CollectItem        => 5,
            GameEventType.CollectSpecialItem => 10,
            _                                => 0
        };
    }
}