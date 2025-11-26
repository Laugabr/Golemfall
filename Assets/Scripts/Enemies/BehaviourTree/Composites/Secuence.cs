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

            foreach (var child in children)
            {
                switch (child.Evaluate())
                {
                    case NodeState.Failure:
                        state = NodeState.Failure;
                        return state;
                    case NodeState.Running:
                        state = NodeState.Running;
                        return state; // opcional: podés cortar acá si querés que se evalúe de a un nodo por tick
                    case NodeState.Success:
                        continue;
                }
            }
            state = NodeState.Success;
            return state;

        }
    }
}
