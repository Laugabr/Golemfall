using UnityEngine;
using Fusion;

public class BossAttackHandler : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NetworkObject groundSpikePrefab;
    [SerializeField] private NetworkObject fallingToothPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] groundPoints;
    [SerializeField] private Transform[] ceilingPoints;
    [SerializeField] private NetworkObject telegraphPrefab;
    [SerializeField] private float telegraphTime = 1.5f;

    // =============================
    //  SUELO
    // =============================
    public void SpawnGroundSpikes()
    {
        if (!Object.HasStateAuthority) return;

        foreach (var point in groundPoints)
        {
            if (point == null) continue;

            var telegraph = Runner.Spawn(
                telegraphPrefab,
                point.position,
                Quaternion.identity
            );

            var telegraphComp = telegraph.GetComponent<TelegraphZone>();

            telegraphComp.Init(telegraphTime, () =>
            {
                Runner.Spawn(
                    groundSpikePrefab,
                    point.position,
                    Quaternion.identity
                );
            });
        }
    }

    // =============================
    //  TECHO
    // =============================
    public void SpawnFallingTeeth()
    {
        if (!Object.HasStateAuthority) return;

        foreach (var point in ceilingPoints)
        {
            if (point == null) continue;

            var telegraph = Runner.Spawn(
                telegraphPrefab,
                point.position,
                Quaternion.identity
            );

            var telegraphComp = telegraph.GetComponent<TelegraphZone>();

            telegraphComp.Init(telegraphTime, () =>
            {
                Runner.Spawn(
                    fallingToothPrefab,
                    point.position,
                    Quaternion.identity
                );
            });
        }
    }
}