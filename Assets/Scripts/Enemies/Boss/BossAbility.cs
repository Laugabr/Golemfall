using UnityEngine;

public abstract class BossAbility : ScriptableObject
{
    public abstract void Execute(BossAttackHandler handler);
}