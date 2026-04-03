using Fusion;
using UnityEngine;

public static class ProjectileRuntime
{
    public static void Execute(
        ProjectileAbility data,
        NetworkRunner runner,
        NetworkObject caster,
        Vector3 direction)
    {
        Debug.Log("EXECUTE PROJECTILE");

        Vector3 spawnPos = caster.transform.position + Vector3.up * 1f;
        Quaternion rot = Quaternion.LookRotation(direction);

        runner.Spawn(
            data.projectilePrefab,
            spawnPos,
            rot,
            inputAuthority: null,
            (runner, obj) =>
            {
                var proj = obj.GetComponent<Projectile>();

                var stats = caster.GetComponent<CharacterStats>();
                float damage = stats.GetStat(Stat.damage) * data.damageMultiplier;

                proj.Initialize(
                    caster,
                    Mathf.FloorToInt(damage),
                    data.projectileSpeed,
                    direction,
                    activeTime: data.activeTime
                );
            }
        );

        Debug.Log("[SERVER] Projectile spawned");
    }
}

