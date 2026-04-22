using BehaviourTree;
using UnityEngine;

public class GroundSpikesAttackNode : Node
{
    private BossAI boss;
    private float cooldown;
    private float lastTime;

    public GroundSpikesAttackNode(BossAI boss, float cooldown)
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

        Debug.Log("[BT] Ground Spikes Attack");

        boss.AttackHandler.SpawnGroundSpikes();

        lastTime = Time.time;
        return NodeState.Success;
    }
}
