using UnityEngine;
using BehaviourTree;

public class CanSeePlayerNode : Node
{
    private Transform enemyTransform;
    private Transform playerTransform;
    private float visionRange;

    public CanSeePlayerNode(Transform enemy, Transform player, float visionRange)
    {
        this.enemyTransform = enemy;
        this.playerTransform = player;
        this.visionRange = visionRange;
    }

    public override NodeState Evaluate()
    {
        float distance = Vector3.Distance(enemyTransform.position, playerTransform.position);

        if (distance <= visionRange)
        {
            state = NodeState.Success;
        }
        else
        {
            state = NodeState.Failure;
        }

        return state;
    }
}

