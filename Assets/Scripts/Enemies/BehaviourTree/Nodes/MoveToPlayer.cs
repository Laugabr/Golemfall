/*using UnityEngine;

namespace BehaviourTree
{
    public class MoveToPlayer : Node
    {
        private EnemyAI _enemyAI;

        public MoveToPlayer(EnemyAI enemyAI)
        {
            _enemyAI = enemyAI;
        }

        public override NodeState Evaluate()
        {
            Vector3 dir = (_enemyAI.Player.position - _enemyAI.transform.position).normalized;
            _enemyAI.transform.position += dir * _enemyAI.moveSpeed * Time.deltaTime;

            float distance = Vector3.Distance(
                _enemyAI.transform.position,
                _enemyAI.Player.position
            );

            if (distance <= _enemyAI.attackRange)
            {
                state = NodeState.Success;
                return state;
            }

            state = NodeState.Running;
            return state;
        }
    }
}*/

