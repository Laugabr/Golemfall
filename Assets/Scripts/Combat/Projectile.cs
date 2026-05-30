using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [Networked] private Vector3 Direction { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private int Damage { get; set; }
    [Networked] private float ActiveTime { get; set; }
    [Networked] private bool hasHit { get; set; }
    [Networked] private NetworkObject Owner { get; set; }
    [Networked] private ProjectileType Type { get; set; }
    [Networked] private bool DestroyOnHit { get; set; }

    [Header("VFX (asignar en Inspector)")]
    [SerializeField] private GameObject collisionVFX;  // efecto al chocar con cualquier cosa
    [SerializeField] private GameObject hitTargetVFX;  // efecto al dañar un objetivo (opcional)

    private HashSet<NetworkObject> hitTargets = new HashSet<NetworkObject>();
    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.enabled = false;
    }

    public override void Spawned()
    {
        // Solo el cliente que disparó destruye su fake
        if (Object.HasStateAuthority || !Object.HasInputAuthority) return;

        // Owner es el NetworkObject del caster (ya lo tenés networkeado)
        uint ownerId = Owner.Id.Raw;
        var fake = FakeProjectileRegistry.Dequeue(ownerId);

        if (fake != null) Destroy(fake.gameObject);
    }
    public void Initialize(NetworkObject caster, int damage, float speed, Vector3 dir,
                           float activeTime, bool destroyOnHit, ProjectileType type)
    {
        Owner = caster;
        Damage = damage;
        Speed = speed;
        Direction = dir.normalized;
        ActiveTime = activeTime;
        DestroyOnHit = destroyOnHit;
        Type = type;

        if (col == null) col = GetComponent<Collider>();
        col.enabled = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        ActiveTime -= Runner.DeltaTime;
        transform.position += Direction * Speed * Runner.DeltaTime;

        ActiveTime -= Runner.DeltaTime;
        transform.position += Direction * Speed * Runner.DeltaTime;

        if (ActiveTime <= 0)
        {
            RPC_SpawnVFX(transform.position, transform.rotation, false);
            Runner.Despawn(Object);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.HasStateAuthority) return;
        if (hasHit) return;
        if (Owner == null) return;

        var otherNet = other.GetComponent<NetworkObject>();
        if (otherNet != null && otherNet == Owner) return;

        // Siempre mostramos el efecto de colisión (pared, suelo, etc.)
        bool damagedTarget = false;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            if (Type == ProjectileType.Player && !other.CompareTag("Enemy")) goto skip;
            if (Type == ProjectileType.Enemy && !other.CompareTag("Player")) goto skip;

            if (otherNet != null)
            {
                if (hitTargets.Contains(otherNet)) return;
                hitTargets.Add(otherNet);
            }

            damageable.TakeDamage(Damage, Owner.gameObject);
            damagedTarget = true;
        }

        skip:
        hasHit = true;

        RPC_SpawnVFX(transform.position, transform.rotation, damagedTarget);

        if (DestroyOnHit)
        {
            col.enabled = false;
            Runner.Despawn(Object);
        }
    }

    /// <summary>
    /// RPC enviado a TODOS los clientes para instanciar VFX localmente.
    /// Cada cliente crea el efecto en su propia máquina — sin NetworkObject ni bandwidth extra.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnVFX(Vector3 position, Quaternion rotation, bool showHitTargetFX)
    {
        // Efecto de colisión (siempre)
        if (collisionVFX != null)
        {
            var vfx = Instantiate(collisionVFX, position, rotation);
            // Auto-destruir tras N segundos si el prefab no lo hace solo
            Destroy(vfx, 5f);
        }

        // Efecto de impacto al objetivo (solo si dañó y tiene el prefab asignado)
        if (showHitTargetFX && hitTargetVFX != null)
        {
            var vfx = Instantiate(hitTargetVFX, position, rotation);
            Destroy(vfx, 5f);
        }
    }
}