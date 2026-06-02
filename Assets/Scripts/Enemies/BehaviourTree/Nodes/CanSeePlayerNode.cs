using UnityEngine;

namespace BehaviourTree
{
    public class CanSeePlayer : Node
    {
        private EnemyAI ai;

        public CanSeePlayer(EnemyAI enemyAI)
        {
            ai = enemyAI;
        }

        public override NodeState Evaluate()
        {
            if (ai.CurrentTarget == null)
                return state = NodeState.Failure;

            float distance = Vector3.Distance(
                ai.transform.position,
                ai.CurrentTarget.position
            );

            if (distance <= ai.VisionRange)
                return state = NodeState.Success;

            return state = NodeState.Failure;
        }
    }
}



