using UnityEngine;
using Fusion;

[CreateAssetMenu(menuName = "Boss/Abilities/Ranged")]
public class BossRangedAbility : BossAbility
{
    [SerializeField] private NetworkPrefabRef projectilePrefab;

    public override void Execute(BossAttackHandler handler)
    {
        Debug.Log("[Ability] RANGED → Spawn SplitProjectile");

        var obj = handler.Runner.Spawn(
            projectilePrefab,
            handler.transform.position,
            handler.transform.rotation
        );

        //  Setear attacker
        var damage = obj.GetComponent<DealDamage>();
        if (damage != null)
        {
            damage.SetAttacker(handler.transform);
        }
        else
        {
            Debug.LogWarning("[Ability] Projectile sin DealDamage");
        }
    }
}
