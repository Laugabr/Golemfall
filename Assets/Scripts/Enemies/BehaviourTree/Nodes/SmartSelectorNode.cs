using UnityEngine;

namespace BehaviourTree
{
    public class SmartSelectorNode : Node
    {
        private BossAI ai;
        private CooldownNode meleeCooldown;
        private CooldownNode rangedCooldown;

        public SmartSelectorNode(BossAI ai, CooldownNode meleeCD, CooldownNode rangedCD)
        {
            this.ai = ai;
            meleeCooldown = meleeCD;
            rangedCooldown = rangedCD;
        }

        public override NodeState Evaluate()
        {
            if (ai.CurrentTarget == null)
                return state = NodeState.Failure;

            float distance = Vector3.Distance(
                ai.transform.position,
                ai.CurrentTarget.position
            );

            // PRIORIDAD: melee
            if (distance <= ai.AttackRange)
            {
                if (meleeCooldown.Evaluate() == NodeState.Success)
                {
                    ai.AttackHandler.ExecuteAbility(0);
                    return state = NodeState.Success;
                }
            }

            // fallback: ranged
            if (distance <= ai.ShootDistance)
            {
                if (rangedCooldown.Evaluate() == NodeState.Success)
                {
                    ai.AttackHandler.ExecuteAbility(1);
                    return state = NodeState.Success;
                }
            }

            return state = NodeState.Failure;
        }
    }
}