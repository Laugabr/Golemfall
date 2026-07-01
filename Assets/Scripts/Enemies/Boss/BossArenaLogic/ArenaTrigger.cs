using Fusion;
using UnityEngine;

public class ArenaTrigger : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI bossAI;
    [SerializeField] private ArenaRespawnManager respawnManager;

    private bool hasActivated = false;

    private void Update()
    {
        // Si el boss ya no está activo (murió o hubo wipe),
        // permitimos volver a activar el trigger.
        if (hasActivated && bossAI != null)
        {
            if (!bossAI.IsActive)
            hasActivated = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (hasActivated) return;
        if (!other.CompareTag("Player")) return;

        hasActivated = true;

        Transform root = other.transform.root;

        Debug.Log($"[Arena] Entra: {other.name} → trayendo al resto del grupo");

        // Cachear spawnpoints antes de moverlos
        respawnManager?.CacheSpawnPoints();

        // Teletransportar al resto del grupo
        respawnManager?.TeleportPlayersIntoArena(root);

        // Activar nuevamente la pelea
        bossAI?.ActivateBoss();
    }
}