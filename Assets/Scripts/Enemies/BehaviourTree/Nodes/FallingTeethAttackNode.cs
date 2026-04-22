using BehaviourTree;
using UnityEngine;

public class FallingTeethAttackNode : Node
{
    private BossAI boss;
    private float cooldown;
    private float lastTime;

    public FallingTeethAttackNode(BossAI boss, float cooldown)
    {
        this.boss = boss;
        this.cooldown = cooldown;
    }

    public override NodeState Evaluate()
    {
        if (Time.time < lastTime + cooldown)
        {
            return NodeState.Failure;
        }

        if (boss.CurrentTarget == null)
            return NodeState.Failure;

        Debug.Log("[BT] Falling Teeth Attack");

        boss.AttackHandler.SpawnFallingTeeth();

        lastTime = Time.time;
        return NodeState.Success;
    }
}
