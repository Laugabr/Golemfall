using BehaviourTree;
using System.Collections.Generic;

namespace BehaviourTree
{
    public class Sequence : Node
    {
        private List<Node> children;
        public Sequence(List<Node> children) { this.children = children; }

        public override NodeState Evaluate()
        {
            bool anyRunning = false;

            foreach (var child in children)
            {
                switch (child.Evaluate())
                {
                    case NodeState.Failure:
                        state = NodeState.Failure;
                        return state;
                    case NodeState.Running:
                        anyRunning = true;
                        break;
                }
            }

            state = anyRunning ? NodeState.Running : NodeState.Success;
            return state;
        }
    }
}
