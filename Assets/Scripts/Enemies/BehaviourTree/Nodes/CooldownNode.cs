using UnityEngine;

namespace BehaviourTree
{
    public class CooldownNode : Node
    {
        private float cooldown;
        private float lastTime;

        public CooldownNode(float cooldown)
        {
            this.cooldown = cooldown;
            lastTime = -999f;
        }

        public override NodeState Evaluate()
        {
            if (Time.time >= lastTime + cooldown)
            {
                lastTime = Time.time;
                return state = NodeState.Success;
            }

            return state = NodeState.Failure;
        }
    }
}