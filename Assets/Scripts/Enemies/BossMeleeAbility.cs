using UnityEngine;
using Fusion;

[CreateAssetMenu(menuName = "Boss/Abilities/Melee")]
public class BossMeleeAbility : BossAbility
{
    [Header("Prefab")]
    [SerializeField] private NetworkPrefabRef burnAreaPrefab;

    [Header("Positioning")]
    [SerializeField] private float distanceFromBoss = 2f;

    public override void Execute(BossAttackHandler handler)
    {
       
        if (handler == null)
        {
            Debug.LogError("[MeleeAbility] Handler es null");
            return;
        }

        var ai = handler.GetComponent<BossAI>();

        if (ai == null)
        {
            Debug.LogError("[MeleeAbility] BossAI no encontrado");
            return;
        }

        if (ai.CurrentTarget == null)
        {
            Debug.Log("[MeleeAbility] No hay target");
            return;
        }

        if (!handler.Object.HasStateAuthority)
        {
            Debug.Log("[MeleeAbility] Sin autoridad");
            return;
        }

        //  Dirección hacia el player
        Vector3 dir = (ai.CurrentTarget.position - handler.transform.position).normalized;

        //  Posición de spawn
        Vector3 spawnPos = handler.transform.position + dir * distanceFromBoss;

        Debug.Log($"[MeleeAbility] Spawneando BurnArea en {spawnPos}");

        //  Spawn en red
        var obj = handler.Runner.Spawn(
            burnAreaPrefab,
            spawnPos,
            Quaternion.identity
        );

        if (obj == null)
        {
            Debug.LogError("[MeleeAbility] Falló el spawn del BurnArea");
            return;
        }

        //  Setear atacante para aggro
        var damage = obj.GetComponent<DealDamage>();

        if (damage != null)
        {
            damage.SetAttacker(handler.transform);
            Debug.Log("[MeleeAbility] Attacker seteado correctamente");
        }
        else
        {
            Debug.LogWarning("[MeleeAbility] BurnArea sin DealDamage");
        }
    }
}