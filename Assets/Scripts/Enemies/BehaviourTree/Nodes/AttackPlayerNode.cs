using UnityEngine;

namespace BehaviourTree
{
    public class AttackPlayer : Node
    {
        private EnemyAI ai;
        private float lastAttackTime;

        public AttackPlayer(EnemyAI enemyAI)
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

            if (distance > ai.AttackRange)
                return state = NodeState.Failure;

            if (ai.CanAttack())
            {
                ai.RangedAttack();
                ai.RegisterAttack();
            }

            return state = NodeState.Success;
        }
    }
}