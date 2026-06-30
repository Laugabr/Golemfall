using Fusion;
using UnityEngine;

/// <summary>
/// Trigger de entrada a la arena del boss.
/// En cuanto el PRIMER player entra:
///   1. Cachea los spawn points actuales de todo el grupo (para el wipe).
///   2. Activa al boss (BossAI.ActivateBoss() — esto cierra la puerta
///      y arranca el tracking de ArenaRespawnManager internamente).
///
/// Setup en escena:
///   - Este componente va en un GameObject con un Collider trigger
///     en la entrada/interior de la arena, y un NetworkObject.
///   - Asignar BossAI y ArenaRespawnManager en el inspector.
/// </summary>
public class ArenaTrigger : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI bossAI;
    [SerializeField] private ArenaRespawnManager respawnManager;

    [Header("Settings")]
    [Tooltip("Si true, el trigger se desactiva después de activar el boss (evita retriggering)")]
    [SerializeField] private bool disableAfterActivation = true;

    private bool hasActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (hasActivated) return;
        if (!other.CompareTag("Player")) return;

        Debug.Log($"[Arena] Entra: {other.name} → activando boss");

        // 1. Cachear los spawn points ANTES de que pase nada
        if (respawnManager != null)
            respawnManager.CacheSpawnPoints();
        else
            Debug.LogWarning("[Arena] ArenaRespawnManager no asignado");

        // 2. Activar el boss (internamente llama ActivateForArena)
        if (bossAI != null)
            bossAI.ActivateBoss();
        else
            Debug.LogError("[Arena] BossAI no asignado en el inspector");

        hasActivated = true;

        if (disableAfterActivation)
            gameObject.SetActive(false);
    }
}