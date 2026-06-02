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
    [SerializeField] private AbilityHolder _abilityHolder;
    [SerializeField] private NetEnemyAnimator _animator;

    [Header("Vision")]
    [SerializeField] private float _visionRange = 8f;

    [Header("Movement")]
    [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private float _chaseSpeed = 5f;

    [Header("Melee Settings")]
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackCooldown = 1f;

    [Header("Ranged Settings")]
    [SerializeField] private float _shootDistance = 8f;
    [SerializeField] private float _minDistance = 4f;

    [Header("Territory")]
    [SerializeField] private float _patrolRadius = 8f;
    [SerializeField] private float _chaseRadius = 15f;
    [SerializeField] private float _returnSpeed = 4f;

    [Header("Patrol")]
    [SerializeField] private float _patrolWaitTime = 2f;

    public float PatrolSpeed => _patrolSpeed;
    public float PatrolRadius => _patrolRadius;
    public float ChaseRadius => _chaseRadius;
    public float ReturnSpeed => _returnSpeed;
    public float PatrolWaitTime => _patrolWaitTime;
    public Vector3 HomePosition { get; private set; }
    public bool HasTarget => _hasTarget;
    private bool _wasHavingTarget;
    public float VisionRange => _visionRange;
    public float AttackRange => _attackRange;
    public float ShootDistance => _shootDistance;
    public float MinDistance => _minDistance;
    public float AttackCooldown => _attackCooldown;
    public NavMeshAgent Agent => _agent;
    public Transform CurrentTarget { get; private set; }

    private double _lastAttackTime = -999;
    private Node rootNode;
    private bool _hasTarget;
    private PatrolNode _patrolNode;
    private Vector3 _lastMoveDirection;

    public bool CanAttack()
    {
        if (Runner == null) return false;
        return Runner.SimulationTime >= _lastAttackTime + _attackCooldown;
    }

    public void RegisterAttack()
    {
        _lastAttackTime = Runner.SimulationTime;
    }

    private void Awake()
    {
        if (_agent == null)
            _agent = GetComponent<NavMeshAgent>();

        if (_abilityHolder == null)
            _abilityHolder = GetComponent<AbilityHolder>();

        if (_animator == null)
            _animator = GetComponent<NetEnemyAnimator>();
    }

    public override void Spawned()
    {
        HomePosition = transform.position;
        Debug.Log($"[ENEMY] Spawned - Name: {gameObject.name}, HomePosition: {HomePosition}, HasStateAuthority: {Object.HasStateAuthority}");

        if (!Object.HasStateAuthority)
        {
            _agent.enabled = false;
            return;
        }

        _agent.enabled = true;
        _agent.speed = _patrolSpeed;
        _agent.autoBraking = false;
        BuildTree();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!_agent.isOnNavMesh) return;

        UpdateTarget();


        // Solo resetea el patrol cuando recién detecta el target
        if (_hasTarget && !_wasHavingTarget)
            _patrolNode?.Reset();

        _wasHavingTarget = _hasTarget;

        // Un solo lugar controla la velocidad
        if (_hasTarget)
            _agent.speed = _chaseSpeed;
        else if (Vector3.Distance(transform.position, HomePosition) > _patrolRadius * 1.5f)
            _agent.speed = _returnSpeed;
        else
            _agent.speed = _patrolSpeed;

        rootNode?.Evaluate();

        if (_agent.hasPath && !_agent.pathPending)
        {
            transform.position += _agent.desiredVelocity * Runner.DeltaTime;
            _agent.nextPosition = transform.position;
        }

        if (_hasTarget && CurrentTarget != null)
        {
            Vector3 dir = CurrentTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    Runner.DeltaTime * 10f
                );
        }

        if (_hasTarget && CurrentTarget != null)
        {
            Vector3 dir = CurrentTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    Runner.DeltaTime * 10f
                );
        }
    }

    public override void Render()
    {
        if (!Object.HasStateAuthority) return;
        if (_agent != null && _agent.isOnNavMesh)
            transform.position = _agent.nextPosition;
    }

    void BuildTree()
    {
        var canSee = new CanSeePlayer(this);
        _patrolNode = new PatrolNode(_agent, this);

        if (_enemyType == EnemyType.Melee)
        {
            var moveTo = new MoveToPlayer(this);
            var attack = new AttackPlayer(this);

            rootNode = new Selector(new List<Node>
            {
                new Sequence(new List<Node> { canSee, moveTo, attack }),
                _patrolNode
            });
        }
        else
        {
            var keepDistance = new KeepDistanceNode(this);
            var moveToShoot = new MoveToShootDistance(this);
            var rangedAttack = new RangedAttackNode(this);

            rootNode = new Selector(new List<Node>
            {
                keepDistance,
                new Sequence(new List<Node> { canSee, moveToShoot, rangedAttack }),
                _patrolNode
            });
        }
    }

    void UpdateTarget()
    {
        if (NetworkController.Instance == null) return;

        if (CurrentTarget != null)
        {
            var health = CurrentTarget.GetComponent<PlayerHealth>();
            bool isDead = health != null && health.IsDead;

            // Pierde el target si el jugador sale del chaseRadius desde HomePosition
            float distFromHome = Vector3.Distance(HomePosition, CurrentTarget.position);
            if (isDead || distFromHome > _chaseRadius)
            {
                CurrentTarget = null;
                _hasTarget = false;
            }
            return;
        }

        // Detecta jugadores dentro del visionRange desde la posición actual del enemigo
        float minDist = float.MaxValue;
        Transform closest = null;

        foreach (var kvp in NetworkController.Instance._players)
        {
            var playerObj = kvp.Value;
            if (playerObj == null) continue;

            var health = playerObj.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead) continue;

            // Detección desde posición actual del enemigo con visionRange
            float dist = Vector3.Distance(transform.position, playerObj.transform.position);
            if (dist < minDist && dist <= _visionRange)
            {
                minDist = dist;
                closest = playerObj.transform;
            }
        }

        CurrentTarget = closest;
        _hasTarget = closest != null;
    }

    public void RangedAttack()
    {
        if (!Object.HasStateAuthority) return;
        if (CurrentTarget == null) return;

        _animator?.TriggerAttack();

        Vector3 dir = (CurrentTarget.position - transform.position).normalized;
        _abilityHolder.TryUseAbility(0, dir);
    }

    public void DisableAI()
    {
        if (!Object.HasStateAuthority) return;

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        CurrentTarget = null;
        enabled = false;
    }
}