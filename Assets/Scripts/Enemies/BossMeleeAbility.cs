using UnityEngine;
using Fusion;

[CreateAssetMenu(menuName = "Boss/Abilities/Melee")]
public class BossMeleeAbility : BossAbility
{
    [SerializeField] private NetworkPrefabRef burnAreaPrefab;
    [SerializeField] private Vector3 offset;

    public override void Execute(BossAttackHandler handler)
    {
        Debug.Log("[Ability] MELEE → Spawn Burn Area");

        handler.Runner.Spawn(
            burnAreaPrefab,
            handler.transform.position + offset,
            Quaternion.identity
        );
    }
}