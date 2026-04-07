using UnityEngine;
using Fusion;

[CreateAssetMenu(menuName = "Boss/Abilities/Melee")]
public class BossMeleeAbility : BossAbility
{
    [SerializeField] private NetworkPrefabRef burnAreaPrefab;
    [SerializeField] private Vector3 offset;

    public override void Execute(BossAttackHandler handler)
    {
        handler.Runner.Spawn(
            burnAreaPrefab,
            handler.transform.position + offset,
            Quaternion.identity
        );
    }
}