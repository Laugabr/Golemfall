using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using BehaviourTree;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;

    [Header("Settings")]
    public float visionRange = 8f;
    public float attackRange = 2f;

    // Behaviour Tree
    private Node rootNode;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Nodos de acción
        var canSeePlayer = new CanSeePlayerNode(transform, player, visionRange);
        var moveToPlayer = new MoveToPlayerNode(agent, player);
        var attackPlayer = new AttackPlayerNode(transform, player, attackRange); // Nuevo nodo de ataque
        var patrol = new PatrolNode(agent); // Nodo de patrulla

        // Secuencia de ataque: ver ? acercarse ? atacar
        var attackSequence = new Sequence(new List<Node> { canSeePlayer, moveToPlayer, attackPlayer });

        // Selector raíz: intentar atacar, si falla patrulla
        rootNode = new Selector(new List<Node> { attackSequence, patrol });
    }

    private void Update()
    {
        rootNode.Evaluate();
    }
}

