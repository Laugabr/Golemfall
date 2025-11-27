using UnityEngine;

namespace BehaviourTree
{
    public class RangedAttackNode : Node
    {
        private EnemyAI ai;
        private float lastAttackTime;

        public RangedAttackNode(EnemyAI enemy)
        {
            ai = enemy;
        }

        public override NodeState Evaluate()
        {
            float distance = Vector3.Distance(ai.transform.position, ai.player.position);

            if (distance > ai.shootDistance)
            {
                state = NodeState.Failure;
                return state;
            }

            if (Time.time >= lastAttackTime + ai.attackCooldown)
            {
                ai.RangedAttack();
                lastAttackTime = Time.time;
            }

            state = NodeState.Success;
            return state;
        }
    }
}
