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

        var obj = handler.Runner.Spawn(
            burnAreaPrefab,
            handler.transform.position + offset,
            Quaternion.identity
        );

        //  Setear attacker para aggro
        var damage = obj.GetComponent<DealDamage>();
        if (damage != null)
        {
            damage.SetAttacker(handler.transform);
        }
        else
        {
            Debug.LogWarning("[Ability] BurnArea sin DealDamage");
        }
    }
}