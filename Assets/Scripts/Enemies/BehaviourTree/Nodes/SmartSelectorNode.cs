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
            {
                Debug.Log("[BT] No target");
                return state = NodeState.Failure;
            }

            float distance = Vector3.Distance(
                ai.transform.position,
                ai.CurrentTarget.position
            );

            Debug.Log($"[BT] Distancia: {distance}");

            // MELEE
            if (distance <= ai.AttackRange)
            {
                Debug.Log("[BT] Intentando MELEE");

                if (meleeCooldown.Evaluate() == NodeState.Success)
                {
                    Debug.Log("[BT] MELEE ejecutado");
                    ai.AttackHandler.ExecuteAbility(0);
                    return state = NodeState.Success;
                }
                else
                {
                    Debug.Log("[BT] MELEE en cooldown");
                }
            }

            // RANGED
            if (distance <= ai.ShootDistance)
            {
                Debug.Log("[BT] Intentando RANGED");

                if (rangedCooldown.Evaluate() == NodeState.Success)
                {
                    Debug.Log("[BT] RANGED ejecutado");
                    ai.AttackHandler.ExecuteAbility(1);
                    return state = NodeState.Success;
                }
                else
                {
                    Debug.Log("[BT] RANGED en cooldown");
                }
            }

            Debug.Log("[BT] Sin acciones disponibles");
            return state = NodeState.Failure;
        }
    }
}