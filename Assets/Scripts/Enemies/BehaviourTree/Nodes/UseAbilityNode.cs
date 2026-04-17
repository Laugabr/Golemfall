namespace BehaviourTree
{
    public class UseAbilityNode : Node
    {
        private BossAI ai;
        private int abilityIndex;

        public UseAbilityNode(BossAI ai, int index)
        {
            this.ai = ai;
            abilityIndex = index;
        }

        public override NodeState Evaluate()
        {
            ai.AttackHandler.ExecuteAbility(abilityIndex);
            return state = NodeState.Success;
        }
    }
}
