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
    // Referencia al animator networked. Se auto-resuelve en Awake() si no se asigna.
    [SerializeField] private NetEnemyAnimator _animator;

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

        if (_abilityHolder == null)
            _abilityHolder = GetComponent<AbilityHolder>();

        if (_animator == null)
            _animator = GetComponent<NetEnemyAnimator>();
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
        if (NetworkController.Instance == null) return;

        float minDist = float.MaxValue;
        Transform closest = null;

        foreach (var kvp in NetworkController.Instance._players)
        {
            var playerObj = kvp.Value;

            if (playerObj == null) continue;

            float dist = Vector3.Distance(transform.position, playerObj.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                closest = playerObj.transform;
            }
        }

        CurrentTarget = closest;
    }

    // Melee
  //  public void DealDamage()
  //  {
//
    //    Debug.Log("⚔️ Melee hit");
    ////}

    // Ranged
    public void RangedAttack()
    {
        if (!Object.HasStateAuthority) return;

        // Disparamos la animación de ataque ANTES de spawn — así el tick stamp
        // se replica en el mismo tick que el daño (decisión 1.a: trigger + spawn
        // en el mismo tick). Si más adelante queremos sincronizar el spawn con
        // un AnimationEvent en mitad del clip, lo movemos al método llamado por
        // ese evento.
        _animator?.TriggerAttack();

        if(_enemyType == EnemyType.Ranged)
        {
            _abilityHolder.TryUseAbility(0, CurrentTarget.position - transform.position);
        }  

        if(_enemyType == EnemyType.Melee)
        {
            _abilityHolder.TryUseAbility(0, CurrentTarget.position - transform.position);
        }
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

    /// <summary>
    /// Detiene la AI y el movimiento del enemigo. La llama EnemyHealth.Die()
    /// para que durante la animación de muerte (delay antes del Despawn) la
    /// BT no siga evaluando ni el agent siga caminando.
    /// Mismo patrón que BossAI.DisableBoss().
    /// </summary>
    public void DisableAI()
    {
        if (!Object.HasStateAuthority) return;

        // Frenar el agent. isOnNavMesh evita warnings si el GO ya no está
        // sobre el navmesh por alguna razón puntual.
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        CurrentTarget = null;

        // enabled = false hace que Fusion ya no llame FixedUpdateNetwork acá.
        enabled = false;
    }
}