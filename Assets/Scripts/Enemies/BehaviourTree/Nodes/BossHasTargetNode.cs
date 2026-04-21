using BehaviourTree;

public class BossHasTargetNode : Node
{
    private BossAI boss;

    public BossHasTargetNode(BossAI boss)
    {
        this.boss = boss;
    }

    public override NodeState Evaluate()
    {
        return boss.CurrentTarget != null ? NodeState.Success : NodeState.Failure;
    }
}