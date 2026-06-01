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

            ai.Agent.speed = ai.MoveSpeed;

            ai.Agent.stoppingDistance =
                ai.AttackRange;

            ai.Agent.SetDestination(
                ai.CurrentTarget.position
            );

            float distance =
                Vector3.Distance(
                    ai.transform.position,
                    ai.CurrentTarget.position
                );

            if (distance <= ai.AttackRange)
            {
                ai.Agent.ResetPath();
                return state = NodeState.Success;
            }

            return state = NodeState.Running;
        }
    }
}

