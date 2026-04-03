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

    public void Initialize(NetworkObject caster, int damage, float speed, Vector3 dir, float activeTime)
    {
        Owner = caster;
        Damage = damage;
        Speed = speed;
        Direction = dir.normalized;
        ActiveTime = activeTime;

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
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (hasHit) return;

        var player = other.GetComponent<NetCharacterController>();
        if (player != null) return;

        if (other.GetComponent<NetworkObject>() == Owner) return;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(Damage, Owner.gameObject);
            hasHit = true;
        }

        Runner.Despawn(Object);
    }
}