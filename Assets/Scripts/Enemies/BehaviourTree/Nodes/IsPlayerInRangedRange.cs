/*using UnityEngine;

namespace BehaviourTree
{
    public class IsPlayerInRangedRange : Node
    {
        private BossAI ai;

        public IsPlayerInRangedRange(BossAI boss)
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

            if (distance <= ai.ShootDistance)
                return state = NodeState.Success;

            return state = NodeState.Failure;
        }
    }
}
*/