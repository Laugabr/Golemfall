using UnityEngine;

namespace BehaviourTree
{
    public class MoveToPlayer : Node
    {
        private EnemyAI ai;

        public MoveToPlayer(EnemyAI enemyAI)
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

            if (distance <= ai.AttackRange)
            {
                ai.Agent.ResetPath();
                ai.Agent.velocity = Vector3.zero;
                return state = NodeState.Success;
            }

            // Siempre actualiza el destino para que el path sea fresco
            ai.Agent.SetDestination(ai.CurrentTarget.position);

            return state = NodeState.Running;
        }
    }
}