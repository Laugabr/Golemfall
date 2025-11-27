using UnityEngine;
using UnityEngine.AI;

namespace BehaviourTree
{
    public class KeepDistanceNode : Node
    {
        private EnemyAI ai;
        private NavMeshAgent agent;

        public KeepDistanceNode(EnemyAI enemyAI)
        {
            ai = enemyAI;
            agent = ai.agent;
        }

        public override NodeState Evaluate()
        {
            float distance = Vector3.Distance(ai.transform.position, ai.player.position);

            if (distance < ai.minDistance)  // distancia mínima
            {
                Vector3 dir = (ai.transform.position - ai.player.position).normalized;
                Vector3 targetPos = ai.transform.position + dir * 3f;

                agent.SetDestination(targetPos);

                state = NodeState.Running;
                return state;
            }

            state = NodeState.Failure;
            return state;
        }
    }
}