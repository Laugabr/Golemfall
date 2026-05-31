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

    [Header("Vision")]
    [SerializeField] private float _visionRange = 8f;
    [SerializeField] private float _loseTargetRange = 12f;

    [Header("Movement")]
    [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private float _chaseSpeed = 5f;

    [Header("Melee Settings")]
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackCooldown = 1f;

    [Header("Ranged Settings")]
    [SerializeField] private float _shootDistance = 8f;
    [SerializeField] private float _minDistance = 4f;

    [Header("Patrol")]
    [SerializeField] private Transform[] _patrolPoints;

    // Solo lectura para nodos
    public float VisionRange => _visionRange;
    public float AttackRange => _attackRange;
    public float ShootDistance => _shootDistance;
    public float MinDistance => _minDistance;
    public float AttackCooldown => _attackCooldown;
    public NavMeshAgent Agent => _agent;
    public Transform CurrentTarget { get; private set; }

    // Cooldown de ataque autoritativo usando SimulationTime
    private double _lastAttackTime = -999;

    public bool CanAttack()
    {
        if (Runner == null) return false;
        return Runner.SimulationTime >= _lastAttackTime + _attackCooldown;
    }

    public void RegisterAttack()
    {
        _lastAttackTime = Runner.SimulationTime;
    }

    private Node rootNode;
    private bool _hasTarget;

    private void Awake()
    {
        if (_agent == null)
            _agent = GetComponent<NavMeshAgent>();

        if (_abilityHolder == null)
            _abilityHolder = GetComponent<AbilityHolder>();
    }

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
        {
            _agent.enabled = false;
            return;
        }

        _agent.enabled = true;
        _agent.speed = _patrolSpeed;
        BuildTree();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        UpdateTarget();

        // Velocidad según si tiene target o no
        _agent.speed = _hasTarget ? _chaseSpeed : _patrolSpeed;

        rootNode?.Evaluate();

        // Rotación hacia el target
        if (CurrentTarget != null)
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

    void BuildTree()
    {
        var canSee = new CanSeePlayer(this);
        var patrol = new PatrolNode(_agent, _patrolPoints);

        if (_enemyType == EnemyType.Melee)
        {
            var moveTo = new MoveToPlayer(this);
            var attack = new AttackPlayer(this);

            rootNode = new Selector(new List<Node>
            {
                new Sequence(new List<Node> { canSee, moveTo, attack }),
                patrol
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
                patrol
            });
        }
    }

    void UpdateTarget()
    {
        if (NetworkController.Instance == null) return;

        // Si ya tiene target, solo lo pierde si se aleja demasiado o muere
        if (CurrentTarget != null)
        {
            var health = CurrentTarget.GetComponent<PlayerHealth>();
            bool isDead = health != null && health.IsDead;
            float dist = Vector3.Distance(transform.position, CurrentTarget.position);

            if (isDead || dist > _loseTargetRange)
            {
                CurrentTarget = null;
                _hasTarget = false;
            }
            return;
        }

        // Busca el jugador vivo más cercano dentro del rango de visión
        float minDist = float.MaxValue;
        Transform closest = null;

        foreach (var kvp in NetworkController.Instance._players)
        {
            var playerObj = kvp.Value;
            if (playerObj == null) continue;

            var health = playerObj.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead) continue;

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

        Vector3 dir = (CurrentTarget.position - transform.position).normalized;
        _abilityHolder.TryUseAbility(0, dir);
    }
}