using UnityEngine;
using Fusion;

[CreateAssetMenu(menuName = "Boss/Abilities/Ranged")]
public class BossRangedAbility : BossAbility
{
    [SerializeField] private NetworkPrefabRef projectilePrefab;

    public override void Execute(BossAttackHandler handler)
    {
        handler.Runner.Spawn(
            projectilePrefab,
            handler.transform.position,
            handler.transform.rotation
        );
    }
}
