using UnityEngine;
using Fusion;
using BehaviourTree;
using System.Collections.Generic;

public class BossAI : NetworkBehaviour
{
    [Header("State")]
    [SerializeField] private bool isActive = false;

    [Header("References")]
    [SerializeField] private BossAttackHandler attackHandler;

    [Header("Aggro System")]
    private Dictionary<Transform, float> aggroTable = new();
    public Transform CurrentTarget { get; private set; }

    [Header("Settings")]
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float shootDistance = 8f;

    public float VisionRange => visionRange;
    public float AttackRange => attackRange;
    public float ShootDistance => shootDistance;
    public BossAttackHandler AttackHandler => attackHandler;

    private Node rootNode;

    // =============================
    // INIT
    // =============================

    private void Start()
    {
        if (!Object.HasStateAuthority) return;

        BuildTree();

        Debug.Log("[BossAI] Inicializado - esperando activación");
    }

    // =============================
    // MAIN LOOP
    // =============================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (!isActive) return; // 🔴 CLAVE

        UpdateTarget();

        if (CurrentTarget != null)
            Debug.Log($"[BossAI] Target actual: {CurrentTarget.name}");

        rootNode?.Evaluate();
    }

    // =============================
    // ACTIVACIÓN DESDE ARENA
    // =============================

    public void ActivateBoss()
    {
        if (!Object.HasStateAuthority) return;
        if (isActive) return;

        Debug.Log("[BossAI] ACTIVADO");

        isActive = true;

        RegisterAllPlayers();
    }

    // =============================
    // REGISTRO DE PLAYERS
    // =============================

    void RegisterAllPlayers()
    {
        foreach (var player in PlayerRegistry.Players)
        {
            RegisterPlayer(player);
        }

        Debug.Log($"[BossAI] Players registrados: {aggroTable.Count}");
    }

    // =============================
    // BEHAVIOUR TREE
    // =============================

    void BuildTree()
    {
        var hasTarget = new BossHasTargetNode(this);

        var groundAttack = new GroundSpikesAttackNode(this, 3f);
        var fallingAttack = new FallingTeethAttackNode(this, 5f);

        var attackSelector = new Selector(new List<Node>
    {
        groundAttack,
        fallingAttack
    });

        rootNode = new Sequence(new List<Node>
    {
        hasTarget,
        attackSelector
    });
    }

    // =============================
    // TARGET SYSTEM
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
        {
            Debug.Log($"[BossAI] Cambio de target → {bestTarget?.name}");
        }

        CurrentTarget = bestTarget;
    }

    // =============================
    // AGGRO
    // =============================

    public void AddAggro(Transform player, float amount)
    {
        if (!Object.HasStateAuthority) return;

        if (!aggroTable.ContainsKey(player))
            aggroTable[player] = 0;

        aggroTable[player] += amount;

        Debug.Log($"[Aggro] {player.name} gana {amount} → Total: {aggroTable[player]}");
    }

    public void RegisterPlayer(Transform player)
    {
        if (!aggroTable.ContainsKey(player))
        {
            aggroTable[player] = 0;
            Debug.Log($"[BossAI] Player registrado: {player.name}");
        }
    }

    public void UnregisterPlayer(Transform player)
    {
        if (aggroTable.ContainsKey(player))
        {
            aggroTable.Remove(player);
            Debug.Log($"[BossAI] Player removido: {player.name}");
        }
    }

    public void DisableBoss()
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log("[BossAI] DESACTIVADO");

        //  deja de pensar
        enabled = false;

        // opcional: limpiar target
        CurrentTarget = null;
    }
}