using UnityEngine;
using UnityEngine.AI;
using Fusion;
using BehaviourTree;
using System.Collections.Generic;

public class EnemyAI : NetworkBehaviour
{
    public enum EnemyType { Melee, Ranged }

    [Header("Type")]
    [SerializeField] private EnemyType _enemyType;

    [Header("References")]
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Transform _shootPoint;

    [Header("Players")]
    private List<Transform> players = new List<Transform>();
    public Transform CurrentTarget { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _visionRange = 8f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _shootDistance = 8f;
    [SerializeField] private float _minDistance = 4f;
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private float _attackCooldown = 1f;

    [Header("Patrol")]
    [SerializeField] private Transform[] _patrolPoints;

    [Header("Projectile")]
    [SerializeField] private NetworkPrefabRef _projectilePrefab;

    // Solo lectura para nodos
    public float VisionRange => _visionRange;
    public float AttackRange => _attackRange;
    public float ShootDistance => _shootDistance;
    public float MinDistance => _minDistance;
    public float AttackCooldown => _attackCooldown;
    public NavMeshAgent Agent => _agent;

    private Node rootNode;

    private void Awake()
    {
        if (_agent == null)
            _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        _agent.speed = _moveSpeed;
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
        var canSee = new CanSeePlayer(this);
        var patrol = new PatrolNode(_agent, _patrolPoints);

        if (_enemyType == EnemyType.Melee)
        {
            var moveTo = new MoveToPlayer(this);
            var attack = new AttackPlayer(this);

            var attackSequence = new Sequence(new List<Node>
            {
                canSee,
                moveTo,
                attack
            });

            rootNode = new Selector(new List<Node>
            {
                attackSequence,
                patrol
            });
        }
        else
        {
            var keepDistance = new KeepDistanceNode(this);
            var moveToShoot = new MoveToShootDistance(this);
            var rangedAttack = new RangedAttackNode(this);

            var attackSequence = new Sequence(new List<Node>
            {
                canSee,
                moveToShoot,
                rangedAttack
            });

            rootNode = new Selector(new List<Node>
            {
                keepDistance,
                attackSequence,
                patrol
            });
        }
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

    // Melee
    public void DealDamage()
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log("⚔️ Melee hit");
    }

    // Ranged
    public void RangedAttack()
    {
        if (!Object.HasStateAuthority) return;

        Runner.Spawn(
            _projectilePrefab,
            _shootPoint.position,
            _shootPoint.rotation
        );
    }

    //  Métodos controlados para modificar players
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
