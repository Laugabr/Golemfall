using UnityEngine;
using Fusion;

public class SplitProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float lifeTime = 2f;

    [Header("Split")]
    [SerializeField] private NetworkPrefabRef splitProjectilePrefab;
    [SerializeField] private int splitCount = 3;
    [SerializeField] private float spreadAngle = 45f;

    private float spawnTime;
    private bool hasSplit = false;

    private DealDamage damage;

    private void Awake()
    {
        damage = GetComponent<DealDamage>();
    }

    public override void Spawned()
    {
        spawnTime = Time.time;
        Debug.Log("[Projectile] Spawned");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        transform.position += transform.forward * speed * Runner.DeltaTime;

        if (!hasSplit && Time.time >= spawnTime + lifeTime)
        {
            Split();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log("[Projectile] Impacto con " + other.name);

        damage.ApplyDamage(other.gameObject);

        if (!hasSplit)
        {
            Split();
        }
    }

    void Split()
    {
        hasSplit = true;

        Debug.Log("[Projectile] SPLIT!");

        float angleStep = spreadAngle / (splitCount - 1);
        float startAngle = -spreadAngle / 2;

        for (int i = 0; i < splitCount; i++)
        {
            float angle = startAngle + angleStep * i;

            Quaternion rot = Quaternion.Euler(0, angle, 0) * transform.rotation;

            var obj = Runner.Spawn(
                splitProjectilePrefab,
                transform.position,
                rot
            );

            //  pasar attacker
            var dmg = obj.GetComponent<DealDamage>();
            if (dmg != null && damage != null)
            {
                dmg.SetAttacker(transform);
            }
        }

        Runner.Despawn(Object);
    }
}
