using UnityEngine;
using UnityEngine.AI;
using Fusion;
using BehaviourTree;
using System.Collections.Generic;

/// <summary>
/// Controlador principal de la IA enemiga con networking via Fusion 2.
/// Solo el host (StateAuthority) corre la lógica de IA y movimiento.
/// Los clientes reciben la posición replicada via NetworkTransform.
///
/// Soporta dos tipos de enemigo:
///   - Melee: se acerca al jugador y ataca cuerpo a cuerpo.
///   - Ranged: mantiene distancia y ataca a distancia.
///
/// Sistema de territorios:
///   - VisionRange: radio desde el enemigo donde detecta al jugador.
///   - PatrolRadius: radio desde HomePosition donde patrulla aleatoriamente.
///   - ChaseRadius: radio desde HomePosition hasta donde persigue al jugador.
/// </summary>
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
    [SerializeField] private Animator _enemyAnimator;

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

    // Propiedades públicas de solo lectura para los nodos del behaviour tree
    public float PatrolSpeed => _patrolSpeed;
    public float PatrolRadius => _patrolRadius;
    public float ChaseRadius => _chaseRadius;
    public float ReturnSpeed => _returnSpeed;
    public float PatrolWaitTime => _patrolWaitTime;
    public float VisionRange => _visionRange;
    public float AttackRange => _attackRange;
    public float ShootDistance => _shootDistance;
    public float MinDistance => _minDistance;
    public float AttackCooldown => _attackCooldown;
    public NavMeshAgent Agent => _agent;
    public Transform CurrentTarget { get; private set; }
    public bool HasTarget => _hasTarget;
    public bool IsInAttackAnimation { get; set; }

    // Posición inicial del enemigo al spawnear, usada como centro del territorio
    public Vector3 HomePosition { get; private set; }

    private double _lastAttackTime = -999;
    private Node rootNode;
    private bool _hasTarget;
    private bool _wasHavingTarget;
    private PatrolNode _patrolNode;
    private Vector3 _lastMoveDirection;

    /// <summary>
    /// Devuelve true si el cooldown de ataque ya pasó.
    /// Usa SimulationTime de Fusion para ser determinístico en red.
    /// </summary>
    public bool CanAttack()
    {
        if (Runner == null) return false;
        return Runner.SimulationTime >= _lastAttackTime + _attackCooldown;
    }

    /// <summary>
    /// Registra el momento del último ataque para el cooldown.
    /// </summary>
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

        if (_enemyAnimator == null)
            _enemyAnimator = GetComponentInChildren<Animator>();
    }

    public override void Spawned()
    {
        // Guardamos la posición inicial como centro del territorio del enemigo
        HomePosition = transform.position;

        if (!Object.HasStateAuthority)
        {
            // Los clientes no corren la IA, solo reciben la posición replicada
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

        // Cuando el enemigo recién detecta un jugador, reseteamos el patrol
        // para que no continúe con el punto de patrulla anterior
        if (_hasTarget && !_wasHavingTarget)
            _patrolNode?.Reset();

        _wasHavingTarget = _hasTarget;

        // La velocidad se controla en un solo lugar según el estado actual
        if (_hasTarget)
            _agent.speed = _chaseSpeed;
        else if (Vector3.Distance(transform.position, HomePosition) > _patrolRadius * 1.5f)
            _agent.speed = _returnSpeed;  // volviendo a casa
        else
            _agent.speed = _patrolSpeed;  // patrullando normal

        rootNode?.Evaluate();

        // Movemos el transform manualmente para que Fusion replique correctamente
        // el NavMeshAgent no mueve el transform solo cuando se usa con networking
        // No se mueve si está ejecutando la animación de ataque
        bool inAttackAnim = IsInAttackAnimation;

        if (_agent.hasPath && !_agent.pathPending && !IsInAttackAnimation)
        {
            transform.position += _agent.desiredVelocity * Runner.DeltaTime;
            _agent.nextPosition = transform.position;
        }
        else if (IsInAttackAnimation)
        {
            // Forzamos velocidad cero para eliminar la inercia
            _agent.velocity = Vector3.zero;
            _agent.nextPosition = transform.position;
        }

        // Rotación suave hacia el jugador cuando hay target activo
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

    /// <summary>
    /// Sincroniza la posición del transform con el NavMeshAgent cada frame.
    /// Corre en todos los peers a framerate de pantalla.
    /// Solo el host lo aplica ya que es quien controla el agente.
    /// </summary>
    public override void Render()
    {
        if (!Object.HasStateAuthority) return;
        if (_agent != null && _agent.isOnNavMesh)
            transform.position = _agent.nextPosition;
    }

    /// <summary>
    /// Construye el árbol de comportamiento según el tipo de enemigo.
    /// Melee: detectar → acercarse → atacar → patrullar
    /// Ranged: mantener distancia → detectar → acercarse al rango → atacar → patrullar
    /// </summary>
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

    /// <summary>
    /// Actualiza el target actual del enemigo cada tick.
    /// Detección: busca jugadores dentro de VisionRange desde la posición actual.
    /// Pérdida: pierde el target si el jugador sale del ChaseRadius desde HomePosition
    /// o si el jugador muere.
    /// </summary>
    void UpdateTarget()
    {
        if (NetworkController.Instance == null) return;

        if (CurrentTarget != null)
        {
            var health = CurrentTarget.GetComponent<PlayerHealth>();
            bool isDead = health != null && health.IsDead;
            float distFromHome = Vector3.Distance(HomePosition, CurrentTarget.position);

            if (isDead || distFromHome > _chaseRadius)
            {
                CurrentTarget = null;
                _hasTarget = false;
            }
            return;
        }

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

    /// <summary>
    /// Dispara la animación de ataque y usa la habilidad del AbilityHolder
    /// apuntando hacia el jugador. Solo corre en el host.
    /// </summary>
    public void RangedAttack()
    {
        if (!Object.HasStateAuthority) return;
        if (CurrentTarget == null) return;

        _animator?.TriggerAttack();

        Vector3 dir = (CurrentTarget.position - transform.position).normalized;
        _abilityHolder.TryUseAbility(0, dir);
    }

    /// <summary>
    /// Detiene la IA y el NavMeshAgent. Se llama desde EnemyHealth.Die()
    /// para que el enemigo no siga atacando durante la animación de muerte.
    /// </summary>
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

    /// <summary>
    /// Dibuja los radios del enemigo en la Scene view cuando está seleccionado.
    /// Amarillo = visión, Verde = patrulla, Rojo = persecución, Magenta = ataque.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 home = Application.isPlaying ? HomePosition : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _visionRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(home, _patrolRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(home, _chaseRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}