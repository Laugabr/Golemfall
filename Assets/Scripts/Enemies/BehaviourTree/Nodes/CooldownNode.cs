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
                Debug.Log($"[Cooldown] Disponible ({cooldown}s)");
                return state = NodeState.Success;
            }

            float remaining = (lastTime + cooldown) - Time.time;
            Debug.Log($"[Cooldown] Restante: {remaining:F2}s");

            return state = NodeState.Failure;
        }
    }
}