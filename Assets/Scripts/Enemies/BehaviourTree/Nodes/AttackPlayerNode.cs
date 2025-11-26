/*using UnityEngine;

namespace BehaviourTree
{
    public class AttackPlayer : Node
    {
        private EnemyAI _enemyAI;

        private float _nextAttackTime;

        public AttackPlayer(EnemyAI enemyAI)
        {
            _enemyAI = enemyAI;
        }

        public override NodeState Evaluate()
        {
            if (Time.time >= _nextAttackTime)
            {
                _enemyAI.DealDamage();
                _nextAttackTime = Time.time + _enemyAI.attackCooldown;
            }

            // Ataque instantáneo → Success
            state = NodeState.Success;
            return state;
        }
    }
}*/
