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
///
/// Integración con PlayerHealth:
///   - ActivateForArena() prende arenaFightActive en cada player trackeado,
///     así Die() los deja muertos en vez de auto-respawnear (ver PlayerHealth.Die()),
///     y cachea su _lastSpawnPoint ACTUAL (el de afuera de la arena) antes de
///     que pase nada más, para poder devolverlos ahí en el wipe.
///   - El wipe (ResetFightRoutine) usa ForceRespawn() con ese punto cacheado —
///     NO con un punto de la arena. Los players nunca respawnean dentro de
///     la arena; esperan muertos hasta que cae el último, y ahí vuelven todos
///     juntos a donde estaban antes de entrar a pelear.
///   - El teleport grupal al ENTRAR a la arena (TeleportPlayersIntoArena) es
///     un caso aparte — ahí sí se usa respawnPoints, porque ese teleport es
///     para meter al grupo adentro, no para revivirlos.
///   - DeactivateArena() apaga arenaFightActive, asi si el boss muere con
///     algún player muerto, ese player vuelve a respawnear solo (normal).
/// </summary>
public class ArenaRespawnManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI bossAI;
    [SerializeField] private BossHealth bossHealth;

    [Header("Respawn Points (dentro de la arena, solo para el teleport de entrada)")]
    [SerializeField] private Transform[] respawnPoints;

    [Tooltip("Delay extra antes de resetear el boss cuando todos mueren")]
    [SerializeField] private float resetDelay = 2f;

    private bool isActive = false;
    private bool isResetting = false;

    // Tracked players y su estado de muerte
    private List<PlayerHealth> trackedPlayers = new();

    // Punto de spawn de cada player ANTES de entrar a la arena (su último
    // checkpoint real). Se cachea al activar la pelea y se usa en el wipe
    // para devolverlos ahí — nunca a un punto dentro de la arena.
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
        originalSpawnPoints.Clear();

        foreach (var playerTransform in PlayerRegistry.Players)
        {
            if (playerTransform == null) continue;
            var health = playerTransform.GetComponent<PlayerHealth>();
            if (health == null) continue;

            trackedPlayers.Add(health);

            // Cacheamos SU spawn point actual (de afuera de la arena) antes
            // de tocar nada más. Este es el punto al que van a volver cuando
            // todos mueran — no uno de la arena.
            originalSpawnPoints[health] = health._lastSpawnPoint;

            // A partir de acá, morir no auto-respawnea — se espera el wipe.
            health.SetArenaFightActive(true);
        }

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

        // Wipe total → reabrimos la arena. Open() es idempotente: si el
        // boss murió casi al mismo tiempo (DisableBoss también la abre),
        // el segundo Open() no hace nada, no hay riesgo de pisarse.
        if (bossAI != null)
            bossAI.RequestOpenArenaGate();

        // 1. Resetear el boss
        bossHealth?.ResetBoss();

        // 2. Revivir + teletransportar a todos los players a su punto de
        //    ANTES de entrar a la arena (cacheado en ActivateForArena),
        //    nunca a un respawnPoint dentro de la arena.
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
    // TELETRANSPORTE AL ENTRAR (nuevo diseño)
    // =============================

    /// <summary>
    /// Teletransporta a todos los players registrados (excepto el que ya
    /// entró caminando, si se indica) a un punto dentro de la arena.
    /// Pensado para llamarse desde ArenaTrigger en cuanto el primer player
    /// entra, así no hace falta que todo el grupo camine hasta la entrada.
    ///
    /// Esto es independiente del sistema de respawn por muerte: simplemente
    /// mueve players vivos al entrar. Usa TeleportTo() (no toca vida/IsDead).
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
    // DESACTIVACIÓN (boss muerto)
    // =============================

    /// <summary>
    /// Llamar cuando el boss muere para restaurar el comportamiento normal
    /// de respawn individual en los players.
    /// </summary>
    /// <summary>
    /// Llamar cuando el boss muere para restaurar el comportamiento normal
    /// de respawn individual en los players. Si hay players muertos
    /// esperando el wipe, los revivimos ahora en su último spawn point.
    /// </summary>
    public void DeactivateArena()
    {
        if (!Object.HasStateAuthority) return;

        // Revivir cualquier player que quedó muerto esperando el wipe.
        // Esto pasa si el boss murió antes de que corra ResetFightRoutine
        // (ej. ambos se mataron al mismo tiempo), o si querés permitir
        // que el boss se derrote sin wipe de todo el grupo.
        foreach (var health in trackedPlayers)
        {
            if (health == null) continue;

            if (health.IsDead)
            {
                Vector3 point = originalSpawnPoints.TryGetValue(health, out var cached)
                    ? cached
                    : health._lastSpawnPoint; // fallback por las dudas

                health.ForceRespawn(point);
                Debug.Log($"[ArenaRespawn] {health.gameObject.name} revivido en punto de salida");
            }
        }

        // Apagamos el flag DESPUÉS de revivir, para que los ForceRespawn() 
        // funcionen normalmente sin interferencia de arenaFightActive.
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