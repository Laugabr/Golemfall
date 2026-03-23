using UnityEngine;

namespace BehaviourTree
{
    public class MeleeAttackNode : Node
    {
        private BossAI ai;
        private float lastAttackTime;

        public MeleeAttackNode(BossAI boss)
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

            // Si no está en rango, falla
            if (distance > ai.AttackRange)
                return state = NodeState.Failure;

            // Cooldown
            if (Time.time >= lastAttackTime + ai.AttackCooldown)
            {
                ai.DealDamage();
                lastAttackTime = Time.time;
            }

            return state = NodeState.Success;
        }
    }
}
