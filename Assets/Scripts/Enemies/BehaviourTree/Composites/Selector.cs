using System.Collections.Generic;

namespace BehaviourTree
{
    public class Selector : Node
    {
        private List<Node> children;
        public Selector(List<Node> children) { this.children = children; }

        public override NodeState Evaluate()
        {
            foreach (var child in children)
            {
                switch (child.Evaluate())
                {
                    case NodeState.Success:
                        state = NodeState.Success;
                        return state;
                    case NodeState.Running:
                        state = NodeState.Running;
                        return state;
                    case NodeState.Failure:
                        continue;
                }
            }
            state = NodeState.Failure;
            return state;
        }
    }
}
