using Fusion;
using UnityEngine;

public static class UtilityRuntime
{
    public static void Execute(UtilityAbilityData data, NetworkRunner runner, NetworkObject caster)
    {
        var stats = caster.GetComponent<CharacterStats>();
        int healAmount = Mathf.FloorToInt(stats.GetStat(Stat.damage) * data.healMultiplier);

        Vector3 spawnPos = caster.transform.position;

        runner.Spawn(
            data.aoePrefab,
            spawnPos,
            Quaternion.identity,
            inputAuthority: null,
            (r, obj) =>
            {
                obj.GetComponent<UtilityAbility>().Initialize(
                    caster,
                    healAmount,
                    data.aoeRadius,
                    data.activeTime
                );
            }
        );
    }
}