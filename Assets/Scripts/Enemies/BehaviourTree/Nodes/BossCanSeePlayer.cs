using UnityEngine;

namespace BehaviourTree
{
    public class BossCanSeePlayer : Node
    {
        private BossAI ai;

        public BossCanSeePlayer(BossAI boss)
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

            return distance <= ai.VisionRange
                ? state = NodeState.Success
                : state = NodeState.Failure;
        }
    }
}



