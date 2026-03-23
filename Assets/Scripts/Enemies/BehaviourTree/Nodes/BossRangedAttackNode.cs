using UnityEngine;

namespace BehaviourTree
{
    public class BossRangedAttackNode : Node
    {
        private BossAI ai;
        private float lastAttackTime;

        public BossRangedAttackNode(BossAI boss)
        {
            ai = boss;
        }

        public override NodeState Evaluate()
        {
            if (ai.CurrentTarget == null)
                return state = NodeState.Failure;

            float distance = Vector3.Distance(
                ai.transform.position,
                ai.CurrentTarget.position
            );

            if (distance > ai.ShootDistance)
                return state = NodeState.Failure;

            if (Time.time >= lastAttackTime + ai.AttackCooldown)
            {
                ai.RangedAttack();
                lastAttackTime = Time.time;
            }

            return state = NodeState.Success;
        }
    }
}
