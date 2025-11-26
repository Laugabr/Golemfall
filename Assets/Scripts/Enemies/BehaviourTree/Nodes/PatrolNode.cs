/*using UnityEngine;

namespace BehaviourTree
{
    public class Patrol : Node
    {
        private EnemyAI _enemyAI;
        private int _currentIndex = 0;

        public Patrol(EnemyAI enemyAI)
        {
            _enemyAI = enemyAI;
        }

        public override NodeState Evaluate()
        {
            if (_enemyAI.patrolPoints.Length == 0)
            {
                state = NodeState.Failure;
                return state;
            }

            Transform target = _enemyAI.patrolPoints[_currentIndex];
            _enemyAI.transform.position = Vector3.MoveTowards(
                _enemyAI.transform.position,
                target.position,
                _enemyAI.moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(_enemyAI.transform.position, target.position) < 0.1f)
            {
                _currentIndex = (_currentIndex + 1) % _enemyAI.patrolPoints.Length;
            }

            state = NodeState.Running;
            return state;
        }
    }
}*/
