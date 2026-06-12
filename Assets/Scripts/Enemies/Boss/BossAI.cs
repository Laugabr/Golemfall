using UnityEngine;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// IA del boss arena. Solo corre en el host (StateAuthority).
///
/// Flujo:
///   1. ArenaTrigger detecta que todos los players entraron → llama ActivateBoss().
///   2. El boss empieza a elegir ataques aleatorios (picos de suelo o dientes del techo).
///   3. BossProximityTrigger: si un player se acerca al weak point N segundos → picos extra.
///   4. Al llegar al 50% de HP → Fase 2: además de los ataques normales,
///      spawnea oleadas de enemigos usando el prefab configurado en el inspector.
/// </summary>
public class BossAI : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BossAttackHandler attackHandler;
    [SerializeField] private BossHealth bossHealth;

    [Header("Attack Timing — Fase 1")]
    [SerializeField] private float phase1MinCooldown = 4f;
    [SerializeField] private float phase1MaxCooldown = 7f;

    [Header("Attack Timing — Fase 2")]
    [SerializeField] private float phase2MinCooldown = 3f;
    [SerializeField] private float phase2MaxCooldown = 6f;

    [Header("Phase 2 — Spawn de enemigos")]
    [Tooltip("Prefab de enemigo a spawnear en fase 2 (ya programado externamente)")]
    [SerializeField] private NetworkObject enemyPrefab;
    [Tooltip("Puntos donde pueden spawnear los enemigos")]
    [SerializeField] private Transform[] enemySpawnPoints;
    [Tooltip("Cuántos enemigos spawear por oleada en fase 2")]
    [SerializeField] private int enemiesPerWave = 2;
    [Tooltip("Cada cuántos ataques normales se lanza una oleada de enemigos")]
    [SerializeField] private int attacksBetweenWaves = 3;

    [Header("Phase Threshold")]
    [Tooltip("Porcentaje de HP (0-1) que activa la fase 2")]
    [SerializeField] private float phase2HealthThreshold = 0.5f;

    // ---- estado networked ----
    [Networked] private bool isActive { get; set; }
    [Networked] private int currentPhase { get; set; }

    // ---- estado local (solo host) ----
    private Dictionary<Transform, float> aggroTable = new();
    private float nextAttackTime;
    private int attacksSinceLastWave = 0;

    // referencias de enemigos vivos para desactivarlos al morir el boss
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
        Debug.Log("[BossAI] ¡FASE 2 ACTIVADA! — oleadas de enemigos habilitadas");
    }

    // =============================
    // ATAQUES
    // =============================

    void TryAttack()
    {
        if (Runner.SimulationTime < nextAttackTime) return;

        // En fase 2, cada N ataques spawnea una oleada de enemigos
        if (currentPhase == 2 && enemyPrefab != null)
        {
            attacksSinceLastWave++;

            if (attacksSinceLastWave >= attacksBetweenWaves)
            {
                attacksSinceLastWave = 0;
                SpawnEnemyWave();
            }
        }

        // Elegir ataque de arena al azar (independiente de la fase)
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
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning("[BossAI] Sin puntos de spawn para enemigos");
            return;
        }

        // Limpiamos referencias nulas de oleadas anteriores
        spawnedEnemies.RemoveAll(e => e == null || !e.IsValid);

        int[] indices = ShuffledIndices(enemySpawnPoints.Length);
        int count = Mathf.Min(enemiesPerWave, enemySpawnPoints.Length);

        Debug.Log($"[BossAI] Oleada fase 2 — spawneando {count} enemigos");

        for (int i = 0; i < count; i++)
        {
            var point = enemySpawnPoints[indices[i]];
            if (point == null) continue;

            var enemy = Runner.Spawn(enemyPrefab, point.position, point.rotation);
            if (enemy != null)
                spawnedEnemies.Add(enemy);
        }
    }

    // =============================
    // ACTIVACIÓN
    // =============================

    public void ActivateBoss()
    {
        if (!Object.HasStateAuthority) return;
        if (isActive) return;

        isActive = true;
        RegisterAllPlayers();
        ScheduleNextAttack();

        Debug.Log("[BossAI] ACTIVADO");
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

        Debug.Log("[BossAI] Proximidad al weak point → Ground Spikes");
        attackHandler.SpawnGroundSpikesRandom(currentPhase);
        ScheduleNextAttack(); // evita solapamiento con el ciclo normal
    }

    // =============================
    // MUERTE DEL BOSS
    // =============================

    public void DisableBoss()
    {
        if (!Object.HasStateAuthority) return;

        isActive = false;
        CurrentTarget = null;

        // Desactivar/desestabilizar todos los enemigos spawneados por el boss
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy == null || !enemy.IsValid) continue;

            // Desactivamos el GameObject — sus propios scripts manejan la muerte
            Runner.Despawn(enemy);
        }

        spawnedEnemies.Clear();

        enabled = false;
        Debug.Log("[BossAI] DESACTIVADO — enemigos de oleada eliminados");
    }

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
}
