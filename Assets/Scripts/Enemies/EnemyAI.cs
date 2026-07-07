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
///
/// NOTA sobre spawn dinámico (ej: BossAI.SpawnEnemyWave):
///   Cuando este enemigo se instancia en runtime en una posición arbitraria,
///   el NavMeshAgent puede no "engancharse" al NavMesh en el mismo frame.
///   Spawned() ahora usa NavMesh.SamplePosition + Agent.Warp para garantizar
///   que el agente arranque sobre la malla, y FixedUpdateNetwork reintenta
///   el enganche si todavía no se logró, en vez de quedar congelado para siempre.
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

    [Header("Ranged Settings")]
    [SerializeField] private float _shootDistance = 8f;
    [SerializeField] private float _minDistance = 4f;

    [Header("Territory")]
    [SerializeField] private float _patrolRadius = 8f;
    [SerializeField] private float _chaseRadius = 15f;
    [SerializeField] private float _returnSpeed = 4f;

    [Header("Patrol")]
    [SerializeField] private float _patrolWaitTime = 2f;

    [Header("NavMesh Snap (spawn dinámico)")]
    [Tooltip("Radio de búsqueda para encontrar NavMesh cerca del punto de spawn")]
    [SerializeField] private float navMeshSnapRadius = 3f;
    [Tooltip("Cuántos ticks reintenta engancharse al NavMesh antes de avisar en consola")]
    [SerializeField] private int navMeshRetryWarningTicks = 60;

    [Header("Boss Settings")]
    [Tooltip("Si es false, recibir daño durante la animación de ataque NO interrumpe el ataque (ideal para bosses).")]
    [SerializeField] private bool _canInterruptAttack = true; // ← NUEVO

    public bool CanInterruptAttack => _canInterruptAttack; // ← NUEVO

    public float PatrolSpeed => _patrolSpeed;
    public float PatrolRadius => _patrolRadius;
    public float ChaseRadius => _chaseRadius;
    public float ReturnSpeed => _returnSpeed;
    public float PatrolWaitTime => _patrolWaitTime;
    public float VisionRange => _visionRange;
    public float AttackRange => _attackRange;
    public float ShootDistance => _shootDistance;
    public float MinDistance => _minDistance;
    public NavMeshAgent Agent => _agent;
    public Transform CurrentTarget { get; private set; }
    public bool HasTarget => _hasTarget;
    public bool IsInAttackAnimation { get; set; }

    public Vector3 HomePosition { get; private set; }

    private Node rootNode;
    private bool _hasTarget;
    private bool _wasHavingTarget;
    private PatrolNode _patrolNode;
    private Vector3 _lastMoveDirection;

    // Contador de ticks esperando engancharse al NavMesh (spawn dinámico)
    private int _navMeshRetryTicks = 0;
    private bool _navMeshWarningLogged = false;

    public bool CanAttack()
    {
        if (_abilityHolder == null) return false;
        return _abilityHolder.IsReady(0);
    }

    public void RegisterAttack() { }

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
        HomePosition = transform.position;

        if (!Object.HasStateAuthority)
        {
            _agent.enabled = false;
            return;
        }

        _agent.enabled = true;
        _agent.speed = _patrolSpeed;
        _agent.autoBraking = false;

        // Intentamos enganchar el agente al NavMesh inmediatamente.
        // Si el punto de spawn no está exactamente sobre la malla (común con
        // spawn dinámico desde puntos colocados a mano), esto lo corrige.
        TrySnapToNavMesh();

        BuildTree();
    }

    /// <summary>
    /// Busca el punto de NavMesh más cercano dentro de navMeshSnapRadius
    /// y mueve el agente ahí con Warp (que no respeta colisiones físicas,
    /// ideal para el primer posicionamiento).
    /// Devuelve true si logró engancharse.
    /// </summary>
    bool TrySnapToNavMesh()
    {
        if (_agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSnapRadius, NavMesh.AllAreas))
        {
            _agent.Warp(hit.position);
            transform.position = hit.position;
            return _agent.isOnNavMesh;
        }

        return false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Si todavía no está enganchado al NavMesh (puede pasar el primer
        // tick tras un spawn dinámico), reintentamos en vez de cortar para
        // siempre. Esto resuelve el caso de enemigos spawneados por BossAI
        // que quedaban congelados sin moverse ni atacar.
        if (!_agent.isOnNavMesh)
        {
            _navMeshRetryTicks++;

            if (!TrySnapToNavMesh())
            {
                if (_navMeshRetryTicks == navMeshRetryWarningTicks && !_navMeshWarningLogged)
                {
                    _navMeshWarningLogged = true;
                    Debug.LogWarning(
                        $"[EnemyAI] '{name}' no logra engancharse al NavMesh tras {_navMeshRetryTicks} ticks. " +
                        $"Verificá que el punto de spawn ({transform.position}) esté sobre el NavMesh bakeado " +
                        $"(radio de búsqueda actual: {navMeshSnapRadius}m).");
                }
                return;
            }
        }

        UpdateTarget();

        // Cuando el enemigo recién detecta un jugador, reseteamos el patrol
        // para que no continúe con el punto de patrulla anterior
        if (_hasTarget && !_wasHavingTarget)
            _patrolNode?.Reset();

        _wasHavingTarget = _hasTarget;

        if (_hasTarget)
            _agent.speed = _chaseSpeed;
        else if (Vector3.Distance(transform.position, HomePosition) > _patrolRadius * 1.5f)
            _agent.speed = _returnSpeed;
        else
            _agent.speed = _patrolSpeed;

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
        if (IsInAttackAnimation) return; // no reevaluar target durante el ataque

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

        foreach (var playerTransform in PlayerRegistry.Players)
        {
            if (playerTransform == null) continue;

            var health = playerTransform.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead) continue;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist < minDist && dist <= _visionRange)
            {
                minDist = dist;
                closest = playerTransform;
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
        if (IsInAttackAnimation) return; // guarda extra por las dudas

        IsInAttackAnimation = true; // <-- clave: bloquea reentradas YA, no esperamos al animator
        _animator?.TriggerAttack();
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
    /// Llamado por EnemyFireAtFrame StateMachineBehaviour en el frame exacto del ataque.
    /// Dispara el proyectil o aplica daño sincronizado con la animación.
    /// Solo corre en el host para mantener autoridad de red.
    /// </summary>
    public void FireProjectile()
    {
        if (!Object.HasStateAuthority) return;
        if (CurrentTarget == null)
        {
            Debug.LogWarning($"[EnemyAI] '{name}' intentó disparar sin target — animación desperdiciada.");
            return;
        }
        Vector3 shootPos = _shootPoint != null ? _shootPoint.position : transform.position + Vector3.up * 1f;
        Vector3 targetPos = CurrentTarget.position + Vector3.up * 1f;

        // Si el jugador está en el suelo, igualamos la Y para disparar horizontal
        var playerHealth = CurrentTarget.GetComponent<PlayerHealth>();
        bool playerIsGrounded = playerHealth != null && !playerHealth.IsDead;

        // Usamos la Y del ShootPoint cuando el jugador está cerca del suelo
        float heightDiff = Mathf.Abs(targetPos.y - shootPos.y);
        if (heightDiff < 1f)
            targetPos.y = shootPos.y;

        Vector3 dir = (targetPos - shootPos).normalized;

        // Los enemigos tienen StateAuthority pero no InputAuthority
        // entonces TryUseAbility nunca llega al RPC
        // Llamamos directamente al método de ejecución del servidor
        _abilityHolder.ExecuteAbilityAuthority(0, dir);
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

        // Radio de snap al NavMesh — útil para verificar si un punto de spawn
        // queda dentro de alcance de la malla
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, navMeshSnapRadius);
    }
}