using UnityEngine;
using Fusion;
using BehaviourTree;
using System.Collections.Generic;

public class BossAI : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private Transform attackPoint;

    [Header("Players")]
    private List<Transform> players = new List<Transform>();
    public Transform CurrentTarget { get; private set; }

    [Header("Settings")]
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float shootDistance = 8f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Prefabs")]
    [SerializeField] private NetworkPrefabRef projectilePrefab;
    [SerializeField] private NetworkPrefabRef burnAreaPrefab;

    // ?? SOLO LECTURA para nodos
    public float VisionRange => visionRange;
    public float AttackRange => attackRange;
    public float ShootDistance => shootDistance;
    public float AttackCooldown => attackCooldown;

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

        var meleeRange = new IsPlayerInMeleeRange(this);
        var meleeAttack = new MeleeAttackNode(this);

        var rangedRange = new IsPlayerInRangedRange(this);
        var rangedAttack = new BossRangedAttackNode(this);

        var meleeSequence = new Sequence(new List<Node>
        {
            canSee,
            meleeRange,
            meleeAttack
        });

        var rangedSequence = new Sequence(new List<Node>
        {
            canSee,
            rangedRange,
            rangedAttack
        });

        rootNode = new Selector(new List<Node>
        {
            meleeSequence,
            rangedSequence
        });
    }

    void UpdateTarget()
    {
        float minDist = float.MaxValue;
        Transform closest = null;

        foreach (var p in players)
        {
            if (p == null) continue;

            float dist = Vector3.Distance(transform.position, p.position);

            if (dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }

        CurrentTarget = closest;
    }

    // ??? Melee
    public void DealDamage()
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log("??? Boss melee!");

        Runner.Spawn(
            burnAreaPrefab,
            attackPoint.position,
            Quaternion.identity
        );
    }

    // ?? Ranged
    public void RangedAttack()
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log("?? Boss ranged!");

        Runner.Spawn(
            projectilePrefab,
            shootPoint.position,
            shootPoint.rotation
        );
    }

    // ?? Registro controlado de jugadores
    public void RegisterPlayer(Transform player)
    {
        if (!players.Contains(player))
            players.Add(player);
    }

    public void UnregisterPlayer(Transform player)
    {
        if (players.Contains(player))
            players.Remove(player);
    }
}
