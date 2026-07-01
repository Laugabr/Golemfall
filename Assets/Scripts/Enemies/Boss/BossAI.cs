using UnityEngine;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// IA principal del boss arena.
/// - Se activa cuando todos los players entran al ArenaTrigger.
/// - Selecciona ataques aleatoriamente entre picos de suelo y dientes del techo.
/// - Fase 2 al llegar al umbral de HP: además spawna oleadas de enemigos.
/// - ResetBoss() reactiva la IA con fase 1 cuando todos los players mueren.
/// Solo el host (StateAuthority) corre la lógica.
/// </summary>
public class BossAI : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BossAttackHandler attackHandler;
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private ArenaRespawnManager respawnManager;

    [Header("Attack Timing — Fase 1")]
    [SerializeField] private float phase1MinCooldown = 4f;
    [SerializeField] private float phase1MaxCooldown = 7f;

    [Header("Attack Timing — Fase 2")]
    [SerializeField] private float phase2MinCooldown = 3f;
    [SerializeField] private float phase2MaxCooldown = 6f;

    [Header("Phase 2 — Spawn de enemigos")]
    [SerializeField] private NetworkObject enemyPrefab;
    [SerializeField] private Transform[] enemySpawnPoints;
    [SerializeField] private int enemiesPerWave = 2;
    [SerializeField] private int attacksBetweenWaves = 3;

    [Header("Phase Threshold")]
    [SerializeField] private float phase2HealthThreshold = 0.5f;

    [Header("Arena Gate")]
    [Tooltip("Prefab (NetworkObject) de la puerta/barrera de la arena")]
    [SerializeField] private NetworkObject arenaGatePrefab;
    [Tooltip("Dónde spawnear la puerta. Si se deja vacío, se usa la posición del propio boss")]
    [SerializeField] private Transform arenaGateSpawnPoint;

    // Instancia única del prefab. Se spawnea una sola vez (lazy, en la
    // primera vez que se necesita abrir/cerrar) y se reutiliza siempre.
    private NetworkObject arenaGateInstance;
    private ArenaGate arenaGate;

    [Networked] private bool isActive { get; set; }
    [Networked] private int currentPhase { get; set; }

    private Dictionary<Transform, float> aggroTable = new();
    private float nextAttackTime;
    private int attacksSinceLastWave = 0;
    private List<NetworkObject> spawnedEnemies = new();

    public Transform CurrentTarget { get; private set; }
    public BossAttackHandler AttackHandler => attackHandler;
    public int CurrentPhase => currentPhase;
    public bool IsActive => isActive;

    // =============================
    // INIT
    // =============================

    public override void Spawned()
    {
        if (!Object.HasStateAuthority) return;

        currentPhase = 1;
        isActive = false;

        // La puerta existe desde el arranque, abierta (estado default),
        // para que los players puedan entrar a la arena la primera vez.
        InitializeArenaGate();

        Debug.Log("[BossAI] Spawned — esperando activación");
    }

    // =============================
    // MAIN LOOP
    // =============================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!isActive) return;

        UpdatePhase();
        UpdateTarget();
        TryAttack();
    }

    // =============================
    // FASE
    // =============================

    void UpdatePhase()
    {
        if (currentPhase == 2) return;
        if (bossHealth == null) return;
        if (bossHealth.HealthPercent > phase2HealthThreshold) return;

        currentPhase = 2;
        Debug.Log("[BossAI] FASE 2 ACTIVADA — oleadas de enemigos habilitadas");
    }

    // =============================
    // ATAQUES
    // =============================

    void TryAttack()
    {
        if (Runner.SimulationTime < nextAttackTime) return;

        if (currentPhase == 2 && enemyPrefab != null)
        {
            attacksSinceLastWave++;
            if (attacksSinceLastWave >= attacksBetweenWaves)
            {
                attacksSinceLastWave = 0;
                SpawnEnemyWave();
            }
        }

        int roll = Random.Range(0, 2);
        if (roll == 0)
        {
            Debug.Log("[BossAI] Ataque: Ground Spikes");
            attackHandler.SpawnGroundSpikesRandom(currentPhase);
        }
        else
        {
            Debug.Log("[BossAI] Ataque: Falling Teeth");
            attackHandler.SpawnFallingTeeth(currentPhase);
        }

        ScheduleNextAttack();
    }

    void ScheduleNextAttack()
    {
        float min = currentPhase == 2 ? phase2MinCooldown : phase1MinCooldown;
        float max = currentPhase == 2 ? phase2MaxCooldown : phase1MaxCooldown;
        float simTime = Runner != null ? Runner.SimulationTime : Time.time;
        nextAttackTime = simTime + Random.Range(min, max);
    }

    // =============================
    // OLEADA DE ENEMIGOS (fase 2)
    // =============================

    void SpawnEnemyWave()
    {
        if (!Object.HasStateAuthority) return;
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0) return;

        spawnedEnemies.RemoveAll(e => e == null || !e.IsValid);

        int[] indices = ShuffledIndices(enemySpawnPoints.Length);
        int count = Mathf.Min(enemiesPerWave, enemySpawnPoints.Length);

        for (int i = 0; i < count; i++)
        {
            var point = enemySpawnPoints[indices[i]];
            if (point == null) continue;

            var enemy = Runner.Spawn(enemyPrefab, point.position, point.rotation);
            if (enemy != null)
                spawnedEnemies.Add(enemy);
        }

        Debug.Log($"[BossAI] Oleada fase 2 — {count} enemigos spawneados");
    }

    // =============================
    // ACTIVACIÓN
    // =============================

        public void ActivateBoss()
    {
        if (!Object.HasStateAuthority) return;
        if (isActive) return;

        currentPhase = 1;
        attacksSinceLastWave = 0;
        nextAttackTime = 0f;

        aggroTable.Clear();

        isActive = true;

        RegisterAllPlayers();
        ScheduleNextAttack();

        attackHandler.InitializeWeakPoint();

        CloseArenaGate();

        if (respawnManager != null)
            respawnManager.ActivateForArena();

        Debug.Log("[BossAI] ACTIVADO");
    }

    // =============================
    // RESET (todos los players mueren)
    // =============================

    /// <summary>
    /// Reactiva el boss desde fase 1. Llamado por BossHealth.ResetBoss()
    /// cuando todos los players mueren y el ArenaRespawnManager lo solicita.
    /// </summary>
    public void ResetBoss()
    {
        if (!Object.HasStateAuthority) return;

        currentPhase = 1;
        isActive = true;
        attacksSinceLastWave = 0;
        aggroTable.Clear();

        // Limpiar enemigos de oleadas anteriores
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy == null || !enemy.IsValid) continue;
            Runner.Despawn(enemy);
        }
        spawnedEnemies.Clear();

        RegisterAllPlayers();
        ScheduleNextAttack();

        // No vuelve a spawnear nada (ya existe), pero por si ResetBoss()
        // se llamara antes de una ActivateBoss() previa, lo cubrimos igual.
        attackHandler.InitializeWeakPoint();

        // ArenaRespawnManager abre la puerta brevemente al detectar el wipe
        // (ver ResetFightRoutine); acá, al reactivar al boss, la volvemos
        // a cerrar — así queda cerrada mientras la pelea esté en curso,
        // sea la primera vez o un reintento.
        CloseArenaGate();

        Debug.Log("[BossAI] Boss reseteado — vuelve a fase 1");
    }

    // =============================
    // TARGET (aggro)
    // =============================

    void UpdateTarget()
    {
        float maxAggro = -1f;
        Transform bestTarget = null;

        foreach (var pair in aggroTable)
        {
            if (pair.Key == null) continue;
            if (pair.Value > maxAggro)
            {
                maxAggro = pair.Value;
                bestTarget = pair.Key;
            }
        }

        if (CurrentTarget != bestTarget)
            Debug.Log($"[BossAI] Nuevo target → {bestTarget?.name}");

        CurrentTarget = bestTarget;
    }

    // =============================
    // AGGRO
    // =============================

    public void AddAggro(Transform player, float amount)
    {
        if (!Object.HasStateAuthority) return;
        if (!aggroTable.ContainsKey(player)) aggroTable[player] = 0;
        aggroTable[player] += amount;
    }

    public void RegisterPlayer(Transform player)
    {
        if (!aggroTable.ContainsKey(player))
            aggroTable[player] = 0;
    }

    public void UnregisterPlayer(Transform player)
    {
        aggroTable.Remove(player);
    }

    void RegisterAllPlayers()
    {
        foreach (var player in PlayerRegistry.Players)
            RegisterPlayer(player);

        Debug.Log($"[BossAI] Players registrados: {aggroTable.Count}");
    }

    // =============================
    // TRIGGER MANUAL (BossProximityTrigger)
    // =============================

    public void TriggerGroundSpikes()
    {
        if (!Object.HasStateAuthority) return;
        if (!isActive) return;

        attackHandler.SpawnGroundSpikesRandom(currentPhase);
        ScheduleNextAttack();
    }

    // =============================
    // MUERTE DEL BOSS
    // =============================

public void DisableBoss()
    {
        if (!Object.HasStateAuthority) return;

        isActive = false;
        CurrentTarget = null;

        foreach (var enemy in spawnedEnemies)
        {
            if (enemy == null || !enemy.IsValid) continue;
            Runner.Despawn(enemy);
        }
        spawnedEnemies.Clear();

        // Apagamos el weak point: deja de moverse y de recibir daño,
        // y se oculta en todos los peers.
        attackHandler.DeactivateWeakPoint();

        // Fin de la pelea: dejamos de trackear muertes/respawn de arena...
        if (respawnManager != null)
            respawnManager.DeactivateArena();

        // ...y reabrimos la puerta. Open() es idempotente (no hace nada si
        // ya estaba abierta), así que no hay problema si en paralelo
        // ArenaRespawnManager también intenta abrirla por un wipe casi
        // simultáneo: el segundo Open() simplemente no hace nada.
        OpenArenaGate();

        enabled = false;
        Debug.Log("[BossAI] DESACTIVADO");
    }

    void InitializeArenaGate()
    {
        if (!Object.HasStateAuthority) return;
        if (arenaGateInstance != null) return;
        if (arenaGatePrefab == null)
        {
            Debug.LogWarning("[BossAI] arenaGatePrefab no asignado");
            return;
        }

        Vector3 pos = arenaGateSpawnPoint != null ? arenaGateSpawnPoint.position : transform.position;
        Quaternion rot = arenaGateSpawnPoint != null ? arenaGateSpawnPoint.rotation : Quaternion.identity;

        arenaGateInstance = Runner.Spawn(arenaGatePrefab, pos, rot);
        arenaGate = arenaGateInstance.GetComponent<ArenaGate>();

        if (arenaGate == null)
            Debug.LogWarning("[BossAI] arenaGatePrefab no tiene componente ArenaGate");
        else
            Debug.Log("[BossAI] Arena gate spawneada");
    }
    

    void OpenArenaGate()
    {
        InitializeArenaGate();
        if (arenaGate != null)
            arenaGate.Open();
    }

    void CloseArenaGate()
    {
        InitializeArenaGate();
        if (arenaGate != null)
            arenaGate.Close();
    }

    /// <summary>
    /// Punto de entrada público para que otros sistemas (ej. ArenaRespawnManager
    /// en un wipe) puedan abrir la puerta sin tener su propia referencia al
    /// prefab/instancia — todo el ciclo de vida de la gate vive acá.
    /// </summary>
    public void RequestOpenArenaGate() => OpenArenaGate();

    // =============================
    // UTILIDAD
    // =============================

    int[] ShuffledIndices(int length)
    {
        int[] indices = new int[length];
        for (int i = 0; i < length; i++) indices[i] = i;
        for (int i = length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return indices;
    }
public void DeactivateAfterWipe()
{
    if (!Object.HasStateAuthority) return;

    isActive = false;
    currentPhase = 1;
    attacksSinceLastWave = 0;
    nextAttackTime = 0f;

    CurrentTarget = null;
    aggroTable.Clear();

    // Despawnear enemigos de las oleadas
    foreach (var enemy in spawnedEnemies)
    {
        if (enemy == null || !enemy.IsValid) continue;
        Runner.Despawn(enemy);
    }

    spawnedEnemies.Clear();

    // Apagar weak point
    attackHandler?.DeactivateWeakPoint();

    // Abrir la puerta
    OpenArenaGate();

    Debug.Log("[BossAI] Pelea cancelada por wipe. Esperando que vuelvan a entrar.");
}
}