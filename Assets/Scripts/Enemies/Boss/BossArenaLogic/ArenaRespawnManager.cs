using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gestiona el respawn de players y detecta cuando todos están muertos
/// para resetear la pelea del boss.
///
/// Setup en escena:
///   - Mismo GO que ArenaTrigger, o GO separado.
///   - Asignar BossAI y BossHealth en el inspector.
///   - Se activa cuando BossAI.ActivateBoss() es llamado (via ActivateForArena()).
///
/// Integración con PlayerHealth:
///   - ActivateForArena() prende arenaFightActive en cada player trackeado,
///     así Die() los deja muertos en vez de auto-respawnear (ver PlayerHealth.Die()).
///   - Cachea el _lastSpawnPoint de cada player ANTES de que entren a la arena,
///     para poder devolverlos ahí en el wipe.
///   - El wipe (ResetFightRoutine) usa ForceRespawn() con ese punto cacheado.
///   - DeactivateArena() apaga arenaFightActive y revive cualquier muerto
///     esperando el wipe (en caso de que el boss muera antes del wipe).
/// </summary>
public class ArenaRespawnManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI bossAI;
    [SerializeField] private BossHealth bossHealth;

    [Tooltip("Delay extra antes de resetear el boss cuando todos mueren")]
    [SerializeField] private float resetDelay = 2f;

    private bool isActive = false;
    private bool isResetting = false;

    // Tracked players y su estado de muerte
    private List<PlayerHealth> trackedPlayers = new();

    // Punto de spawn de cada player ANTES de entrar a la arena (su último
    // checkpoint real). Se cachea al activar la pelea y se usa en el wipe
    // para devolverlos ahí.
    private Dictionary<PlayerHealth, Vector3> originalSpawnPoints = new();

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
        // No limpiar originalSpawnPoints — ya fue cacheado en CacheSpawnPoints()

        foreach (var playerTransform in PlayerRegistry.Players)
        {
            if (playerTransform == null) continue;
            var health = playerTransform.GetComponent<PlayerHealth>();
            if (health != null)
                trackedPlayers.Add(health);
        }

        // A partir de acá, morir no auto-respawnea — se espera el wipe.
        foreach (var health in trackedPlayers)
            health.SetArenaFightActive(true);

        Debug.Log($"[ArenaRespawn] Activado — tracking {trackedPlayers.Count} players");
    }

    // =============================
    // CACHEO DE SPAWN POINTS
    // =============================

    /// <summary>
    /// Cachea los spawn points ACTUALES de cada player ANTES de que sean
    /// teletransportados/entren a la arena. Llamar antes de que pase nada
    /// para garantizar que se guarden los puntos "verdaderos" de afuera.
    /// </summary>
    public void CacheSpawnPoints()
    {
        if (!Object.HasStateAuthority) return;

        originalSpawnPoints.Clear();

        foreach (var playerTransform in PlayerRegistry.Players)
        {
            if (playerTransform == null) continue;
            var health = playerTransform.GetComponent<PlayerHealth>();
            if (health == null) continue;

            // Guardar el punto ACTUAL antes de que sea pisado por nada
            originalSpawnPoints[health] = health._lastSpawnPoint;
            Debug.Log($"[ArenaRespawn] Cached spawn point para {health.gameObject.name}: {health._lastSpawnPoint}");
        }
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

        // Wipe total → reabrimos la arena
        if (bossAI != null)
            bossAI.RequestOpenArenaGate();

        // 1. Resetear el boss
        bossHealth?.ResetBoss();

        // 2. Revivir + teletransportar a todos los players a su punto de
        //    ANTES de entrar a la arena (cacheado en CacheSpawnPoints).
        foreach (var health in trackedPlayers)
        {
            if (health == null) continue;

            Vector3 point = originalSpawnPoints.TryGetValue(health, out var cached)
                ? cached
                : health._lastSpawnPoint; // fallback por las dudas

            health.ForceRespawn(point);
        }

        yield return new WaitForSeconds(0.5f);

        isResetting = false;

        Debug.Log("[ArenaRespawn] Pelea reseteada");
    }

    // =============================
    // DESACTIVACIÓN (boss muerto)
    // =============================

    /// <summary>
    /// Llamar cuando el boss muere para restaurar el comportamiento normal
    /// de respawn individual en los players. Si hay players muertos
    /// esperando el wipe, los revivimos ahora en su último spawn point.
    /// </summary>
    public void DeactivateArena()
    {
        if (!Object.HasStateAuthority) return;
        if (isResetting) return; // guard: si está reseteando la pelea, no interfieran

        // Revivir cualquier player que quedó muerto esperando el wipe.
        foreach (var health in trackedPlayers)
        {
            if (health == null) continue;

            if (health.IsDead)
            {
                Vector3 point = originalSpawnPoints.TryGetValue(health, out var cached)
                    ? cached
                    : health._lastSpawnPoint;

                health.ForceRespawn(point);
                Debug.Log($"[ArenaRespawn] {health.gameObject.name} revivido en punto de salida");
            }
        }

        // Apagamos el flag para que vuelva el comportamiento normal de respawn
        foreach (var health in trackedPlayers)
        {
            if (health == null) continue;
            health.SetArenaFightActive(false);
        }

        isActive = false;
        isResetting = false;
        trackedPlayers.Clear();
        originalSpawnPoints.Clear();

        Debug.Log("[ArenaRespawn] Arena desactivada — boss muerto, players revividos");
    }
}