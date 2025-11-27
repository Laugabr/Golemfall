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
            float distance = Vector3.Distance(ai.transform.position, ai.player.position);

            if (distance > ai.shootDistance)
            {
                ai.agent.SetDestination(ai.player.position);
                state = NodeState.Running;
                return state;
            }

            ai.agent.ResetPath();
            state = NodeState.Success;
            return state;
        }
    }
}
