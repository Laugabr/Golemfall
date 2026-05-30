using UnityEngine;

namespace BehaviourTree
{
    public class MoveToShootDistance : Node
    {
        private EnemyAI ai;

        public MoveToShootDistance(EnemyAI enemy)
        {
            ai = enemy;
        }

        public override NodeState Evaluate()
        {
            if (ai.CurrentTarget == null)
                return state = NodeState.Failure;

            ai.Agent.speed = ai.MoveSpeed;

            ai.Agent.stoppingDistance =
                ai.ShootDistance;

            float distance =
                Vector3.Distance(
                    ai.transform.position,
                    ai.CurrentTarget.position
                );

            if (distance > ai.ShootDistance)
            {
                ai.Agent.SetDestination(
                    ai.CurrentTarget.position
                );

                return state = NodeState.Running;
            }

            ai.Agent.ResetPath();

            return state = NodeState.Success;
        }
    }
}
