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

    private HashSet<NetworkObject> hitTargets = new HashSet<NetworkObject>();
    private Collider col;
    [Networked] private bool DestroyOnHit { get; set; }    private void Awake()
    {
        col = GetComponent<Collider>();
        col.enabled = false; // desactivar al inicio
    }
    public void Initialize(NetworkObject caster, int damage, float speed, Vector3 dir, float activeTime, bool destroyOnHit, ProjectileType type)
    {
        Owner = caster;
        Damage = damage;
        Speed = speed;
        Direction = dir.normalized;
        ActiveTime = activeTime;
        DestroyOnHit = destroyOnHit;
        Type = type;

        if (col == null)
            col = GetComponent<Collider>();

        col.enabled = true;
    }
    public override void Spawned()
    {
        Debug.Log($"SPAWNED en player: {Runner.LocalPlayer}");


    }
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        ActiveTime = ActiveTime - Runner.DeltaTime;
        transform.position += Direction * Speed * Runner.DeltaTime;

        if (ActiveTime <= 0)
        {
            Runner.Despawn(Object);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Validaciones básicas de red
        if (Object == null || !Object.HasStateAuthority) return;
        if (hasHit) return;
        if (Owner == null) return;

        // Evitar pegarle al propio caster
        var otherNet = other.GetComponent<NetworkObject>();
        if (otherNet != null && otherNet == Owner) return;

        // Obtener damageable
        var damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;

        // --- FILTRO PvE ---
        // Player projectile → solo daña enemigos
        if (Type == ProjectileType.Player && !other.CompareTag("Enemy"))
            return;

        // Enemy projectile → solo daña player
        if (Type == ProjectileType.Enemy && !other.CompareTag("Player"))
            return;

        // Evitar múltiples hits al mismo target
        if (otherNet != null)
        {
            if (hitTargets.Contains(otherNet))
                return;

            hitTargets.Add(otherNet);
        }

        // Aplicar daño
        damageable.TakeDamage(Damage, Owner.gameObject);

        // Marcar impacto (por si querés usarlo después)
        hasHit = true;

        // Destruir si corresponde
        if (DestroyOnHit)
        {
            col.enabled = false;
            Runner.Despawn(Object);
        }
    }
}