using UnityEngine;
using Fusion;
using BehaviourTree;
using System.Collections.Generic;

public class BossAI : NetworkBehaviour
{
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

    private void Start()
    {
        BuildTree();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        UpdateTarget();
        rootNode?.Evaluate();
    }

    void BuildTree()
    {
        var canSee = new BossCanSeePlayer(this);

        var meleeCooldown = new CooldownNode(2f);
        var rangedCooldown = new CooldownNode(3f);

        var smartSelector = new SmartSelectorNode(this, meleeCooldown, rangedCooldown);

        rootNode = new Sequence(new List<Node>
        {
            canSee,
            smartSelector
        });
    }

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

        CurrentTarget = bestTarget;
    }

    // AGGRO METHODS

    public void AddAggro(Transform player, float amount)
    {
        if (!Object.HasStateAuthority) return;

        if (!aggroTable.ContainsKey(player))
            aggroTable[player] = 0;

        aggroTable[player] += amount;
    }

    public void RegisterPlayer(Transform player)
    {
        if (!aggroTable.ContainsKey(player))
            aggroTable[player] = 0;
    }

    public void UnregisterPlayer(Transform player)
    {
        if (aggroTable.ContainsKey(player))
            aggroTable.Remove(player);
    }
}
