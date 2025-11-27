using UnityEngine;
using UnityEngine.AI;
using BehaviourTree;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("General References")]
    public Transform player;
    public NavMeshAgent agent;

    [Header("General Settings")]
    public float visionRange = 8f;
    public float moveSpeed = 3.5f;

    [Header("Melee Settings")]
    public bool isRanged = false;     // false = melee, true = ranged/archer
    public float attackRange = 2f;    // usado por AttackPlayer (melee)

    [Header("Ranged Settings")]
    public float shootDistance = 8f;  // distancia ideal para disparar
    public float minDistance = 4f;    // si el jugador está < minDistance, alejarse
    public float attackCooldown = 1.5f; // cooldown común (melee o ranged)

    [Header("Patrol")]
    public Transform[] patrolPoints;

    // Behaviour Tree
    private Node rootNode;

    private void Awake()
    {
        // Si el agente no está asignado por inspector, lo buscamos
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Aseguramos velocidad del agent
        if (agent != null)
            agent.speed = moveSpeed;

        BuildTree();
    }

    private void Update()
    {
        // Actual: protegemos rootNode nulo
        rootNode?.Evaluate();
    }

    private void BuildTree()
    {
        // Nodos comunes
        var canSee = new CanSeePlayer(this);      // debe usar this.player y this.visionRange
        var patrol = new PatrolNode(agent, patrolPoints); // usa NavMeshAgent + patrolPoints

        if (isRanged)
        {
            // Nodos para arquero (Keep distance, mover a distancia de disparo, atacar a distancia)
            var keepDistance = new KeepDistanceNode(this);         // usa minDistance
            var moveToShoot = new MoveToShootDistance(this);      // usa shootDistance y agent
            var rangedAttack = new RangedAttackNode(this);        // usa shootDistance y attackCooldown + RangedAttack()

            var attackSeq = new Sequence(new List<Node>
            {
                canSee,
                moveToShoot,
                rangedAttack
            });

            // Prioridad: primero alejar si está demasiado cerca, luego atacar si puede, sino patrulla
            rootNode = new Selector(new List<Node> { keepDistance, attackSeq, patrol });
        }
        else
        {
            // Nodos para melee (ver → acercarse → atacar)
            var moveTo = new MoveToPlayer(this);      // usa agent and attackRange
            var attack = new AttackPlayer(this);      // usa attackRange, attackCooldown, DealDamage()

            var attackSeq = new Sequence(new List<Node>
            {
                canSee,
                moveTo,
                attack
            });

            rootNode = new Selector(new List<Node> { attackSeq, patrol });
        }
    }

    
    // Métodos que los nodos esperan
    

    // Método llamado por AttackPlayer (melee)
    public void DealDamage()
    {
        Debug.Log($"{name}: DealDamage called");
        // IMPLEMENTAR: daño real al player (ej. player.GetComponent<Health>().TakeDamage(...))
    }

    // Método llamado por RangedAttackNode (archer)
    public void RangedAttack()
    {
        Debug.Log($"{name}: RangedAttack called");
        // IMPLEMENTAR: instanciar proyectil, setear dirección, velocidad y daño
    }

    // helper: comprobar si player está dentro de visionRange
    public bool PlayerInVision()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= visionRange;
    }
}
