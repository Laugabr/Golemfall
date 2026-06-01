using UnityEngine;

namespace BehaviourTree
{
    public class KeepDistanceNode : Node
    {
        private EnemyAI ai;

        public KeepDistanceNode(EnemyAI enemyAI)
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

            if (distance < ai.MinDistance)
            {
                Vector3 dir = (ai.transform.position - ai.CurrentTarget.position).normalized;
                Vector3 targetPos = ai.transform.position + dir * 3f;

                ai.Agent.SetDestination(targetPos);
                return state = NodeState.Running;
            }

            return state = NodeState.Failure;
        }
    }
}