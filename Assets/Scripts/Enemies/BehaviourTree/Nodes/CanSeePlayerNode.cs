/*using UnityEngine;

namespace BehaviourTree
{
    public class CanSeePlayer : Node
    {
        private EnemyAI _enemyAI;

        public CanSeePlayer(EnemyAI enemyAI)
        {
            _enemyAI = enemyAI;
        }

        public override NodeState Evaluate()
        {
            float distance = Vector3.Distance(
                _enemyAI.transform.position,
                _enemyAI.Player.position
            );

            if (distance <= _enemyAI.viewRange)
            {
                state = NodeState.Success;
                return state;
            }

            state = NodeState.Failure;
            return state;
        }
    }
}*/


