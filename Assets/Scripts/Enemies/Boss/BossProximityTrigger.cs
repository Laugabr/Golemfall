using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class BossProximityTrigger : NetworkBehaviour
{
    [SerializeField] private BossAI bossAI;
    [SerializeField] private float cooldown = 5f;

    private bool isOnCooldown = false;

    // evita múltiples activaciones por colliders del mismo player
    private HashSet<Transform> playersInside = new();

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (!other.CompareTag("Player")) return;

        Transform root = other.transform.root;

        // evita múltiples triggers por un mismo player
        if (playersInside.Contains(root)) return;

        playersInside.Add(root);

        if (isOnCooldown) return;

        Debug.Log("[Trigger] Player cerca del weak point → activar spikes");

        isOnCooldown = true;

        bossAI.TriggerGroundSpikes();

        Invoke(nameof(ResetCooldown), cooldown);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Transform root = other.transform.root;
        playersInside.Remove(root);
    }

    void ResetCooldown()
    {
        isOnCooldown = false;
    }
}