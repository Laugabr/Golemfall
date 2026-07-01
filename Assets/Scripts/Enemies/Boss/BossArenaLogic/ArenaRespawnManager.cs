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
///   - Asignar BossAI, BossHealth y los respawnPoints (dentro de la arena)
///     en el inspector.
///   - Se activa cuando BossAI.ActivateBoss() es llamado (via ActivateForArena()).
///
/// Integración con PlayerHealth:
///   - CacheSpawnPoints() guardacada player su _lastSpawnPoint ACTUAL
///     (de afuera de la arena) ANTES de que sea teletransportado.
///   - ActivateForArena() prende arenaFightActive en cada player trackeado,
///     así Die() los deja muertos en vez de auto-respawnear.
///   - El wipe (ResetFightRoutine) usa ForceRespawn() con los puntos cacheados
///     (los de AFUERA), no los de adentro.
///   - DeactivateArena() apaga arenaFightActive y revive cualquier muerto
///     esperando el wipe.
/// </summary>
public class ArenaRespawnManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI bossAI;
    [SerializeField] private BossHealth bossHealth;

    [Header("Respawn Points (dentro de la arena, para teleport de entrada)")]
    [SerializeField] private Transform[] respawnPoints;

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
    // CACHEO DE SPAWN POINTS
    // =============================

    /// <summary>
    /// Cachea los spawn points ACTUALES de cada player ANTES de que sean
    /// teletransportados/entren a la arena. Llamar antes de TeleportPlayersIntoArena()
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
    // TELETRANSPORTE AL ENTRAR
    // =============================

    /// <summary>
    /// Teletransporta a todos los players registrados (excepto el que ya
    /// entró caminando, si se indica) a un punto dentro de la arena.
    /// Pensado para llamarse desde ArenaTrigger en cuanto el primer player
    /// entra, así no hace falta que todo el grupo camine hasta la entrada.
    ///
    /// Usa TeleportTo(), que solo mueve sin tocar vida/IsDead.
    /// </summary>
    public void TeleportPlayersIntoArena(Transform exclude)
    {
        if (!Object.HasStateAuthority) return;
        if (respawnPoints == null || respawnPoints.Length == 0)
        {
            Debug.LogWarning("[ArenaRespawn] Sin puntos de respawn configurados, no se puede teletransportar al grupo");
            return;
        }

        int pointIndex = 0;
        int teleported = 0;

        foreach (var playerTransform in PlayerRegistry.Players)
        {
            if (playerTransform == null) continue;
            if (playerTransform == exclude) continue; // este ya entró caminando

            var health = playerTransform.GetComponent<PlayerHealth>();
            if (health == null) continue;

            Vector3 point = respawnPoints[pointIndex % respawnPoints.Length].position;
            health.TeleportTo(point);

            pointIndex++;
            teleported++;
        }

        Debug.Log($"[ArenaRespawn] Teletransportados {teleported} players al resto del grupo dentro de la arena");
    }

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

        if (bossAI != null)
        {
            bossAI.DeactivateAfterWipe();
        }

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

        // Apagar el modo arena de todos los players
        foreach (var health in trackedPlayers)
        {
            if (health == null) continue;
            health.SetArenaFightActive(false);
        }

        // La pelea terminó.
        // El trigger volverá a iniciar todo cuando alguien entre otra vez.
        isActive = false;
        isResetting = false;

        trackedPlayers.Clear();
        originalSpawnPoints.Clear();

        Debug.Log("[ArenaRespawn] Wipe completo. Esperando que vuelvan a entrar.");
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
                StopAllCoroutines();

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