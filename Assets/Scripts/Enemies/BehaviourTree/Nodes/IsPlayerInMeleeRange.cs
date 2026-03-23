using UnityEngine;

namespace BehaviourTree
{
    public class IsPlayerInMeleeRange : Node
    {
        private BossAI ai;

        public IsPlayerInMeleeRange(BossAI boss)
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

            return distance <= ai.AttackRange
                ? state = NodeState.Success
                : state = NodeState.Failure;
        }
    }
}
