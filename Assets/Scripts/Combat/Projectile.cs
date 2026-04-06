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

    private HashSet<NetworkObject> hitTargets = new HashSet<NetworkObject>();
    private Collider col;
    [Networked] private bool DestroyOnHit { get; set; }    private void Awake()
    {
        col = GetComponent<Collider>();
        col.enabled = false; // desactivar al inicio
    }
    public void Initialize(NetworkObject caster, int damage, float speed, Vector3 dir, float activeTime, bool destroyOnHit)
    {
        Owner = caster;
        Damage = damage;
        Speed = speed;
        Direction = dir.normalized;
        ActiveTime = activeTime;
        DestroyOnHit = destroyOnHit;

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
        Debug.Log($"[SERVER] Projectile colisionó con {other.gameObject.name}");

        if (Object == null) return;
        if (!Object.HasStateAuthority) return;
        if (hasHit) return;
        if (Owner == null) return;

        var player = other.GetComponent<NetCharacterController>();
        if (player != null) return;

        var otherNet = other.GetComponent<NetworkObject>();
        if (otherNet != null && otherNet == Owner) return;

        var damageable = other.GetComponent<IDamageable>();
        
        if (damageable != null)
        {
            if (otherNet != null && hitTargets.Contains(otherNet))
                return;

            if (otherNet != null)
                hitTargets.Add(otherNet);

            damageable.TakeDamage(Damage, Owner.gameObject);

            if (DestroyOnHit)
            {
                col.enabled = false;
                Runner.Despawn(Object);
            }
        }
    }
}