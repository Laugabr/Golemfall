namespace BehaviourTree
{
    public enum NodeState
    {
        Running,
        Success,
        Failure
    }

    public abstract class Node
    {
        protected NodeState state;
        public NodeState State => state;
        public abstract NodeState Evaluate();
    }
}
