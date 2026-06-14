using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gestiona el respawn de players dentro de la arena y detecta
/// cuando todos están muertos para resetear la pelea.
///
/// Setup en escena:
///   - Mismo GO que ArenaTrigger, o GO separado.
///   - Asignar BossAI, BossHealth y los puntos de respawn en el inspector.
///   - Se activa cuando BossAI.ActivateBoss() es llamado (via ActivateForArena()).
/// </summary>
public class ArenaRespawnManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI bossAI;
    [SerializeField] private BossHealth bossHealth;

    [Header("Respawn Points (dentro de la arena)")]
    [SerializeField] private Transform[] respawnPoints;

    [Header("Settings")]
    [SerializeField] private float respawnDelay = 4f;
    [Tooltip("Delay extra antes de resetear el boss cuando todos mueren")]
    [SerializeField] private float resetDelay = 2f;

    private bool isActive = false;
    private bool isResetting = false;

    // Tracked players y su estado de muerte
    private List<PlayerHealth> trackedPlayers = new();

    // =============================
    // ACTIVACIÓN
    // =============================

    /// <summary>
    /// Llamar esto desde ArenaTrigger cuando el boss se activa.
    /// </summary>
    public void ActivateForArena()
    {
        if (!Object.HasStateAuthority) return;
        if (isActive) return;

        isActive = true;
        isResetting = false;

        // Recolectar todos los PlayerHealth del registro
        trackedPlayers.Clear();
        foreach (var playerTransform in PlayerRegistry.Players)
        {
            if (playerTransform == null) continue;
            var health = playerTransform.GetComponent<PlayerHealth>();
            if (health != null)
                trackedPlayers.Add(health);
        }

        // Setear el spawn point de arena para cada player
        SetArenaSpawnPoints();

        Debug.Log($"[ArenaRespawn] Activado — tracking {trackedPlayers.Count} players");
    }

    // =============================
    // MAIN LOOP
    // =============================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!isActive) return;
        if (isResetting) return;

        CheckAllDead();
    }

    // =============================
    // DETECCIÓN DE MUERTE
    // =============================

    void CheckAllDead()
    {
        if (trackedPlayers.Count == 0) return;

        bool allDead = true;
        foreach (var health in trackedPlayers)
        {
            if (health == null) continue;
            if (!health.IsDead)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            Debug.Log("[ArenaRespawn] Todos los players muertos → reseteando pelea");
            isResetting = true;
            StartCoroutine(ResetFightRoutine());
        }
    }

    // =============================
    // RESET DE PELEA
    // =============================

    IEnumerator ResetFightRoutine()
    {
        // Esperamos un momento antes de resetear para que se vea la muerte
        yield return new WaitForSeconds(resetDelay);

        if (!Object.HasStateAuthority) yield break;

        // 1. Resetear el boss
        bossHealth?.ResetBoss();

        // 2. Respawnear a todos los players en la arena
        foreach (var health in trackedPlayers)
        {
            if (health == null) continue;

            // Forzamos respawn inmediato en punto de arena
            Vector3 point = GetRespawnPoint();
            health.SetLastSpawnPoint(point);
            health.NeedsRespawn = true;
        }

        yield return new WaitForSeconds(0.5f);

        isResetting = false;

        Debug.Log("[ArenaRespawn] Pelea reseteada");
    }

    // =============================
    // RESPAWN INDIVIDUAL
    // =============================

    /// <summary>
    /// Setea el punto de spawn de arena para cada player al comenzar la pelea.
    /// De esta forma, cuando el sistema de PlayerHealth haga el respawn normal,
    /// el player aparece dentro de la arena en lugar de en el último checkpoint.
    /// </summary>
    void SetArenaSpawnPoints()
    {
        if (!Object.HasStateAuthority) return;
        if (respawnPoints == null || respawnPoints.Length == 0)
        {
            Debug.LogWarning("[ArenaRespawn] Sin puntos de respawn configurados");
            return;
        }

        for (int i = 0; i < trackedPlayers.Count; i++)
        {
            var health = trackedPlayers[i];
            if (health == null) continue;

            Vector3 point = respawnPoints[i % respawnPoints.Length].position;
            health.SetLastSpawnPoint(point);
        }
    }

    Vector3 GetRespawnPoint()
    {
        if (respawnPoints == null || respawnPoints.Length == 0)
            return Vector3.zero;

        return respawnPoints[Random.Range(0, respawnPoints.Length)].position;
    }

    // =============================
    // DESACTIVACIÓN (boss muerto)
    // =============================

    /// <summary>
    /// Llamar cuando el boss muere para restaurar los spawn points
    /// originales de los players fuera de la arena.
    /// </summary>
    public void DeactivateArena()
    {
        if (!Object.HasStateAuthority) return;

        isActive = false;
        isResetting = false;
        trackedPlayers.Clear();

        Debug.Log("[ArenaRespawn] Arena desactivada — boss muerto");
    }
}