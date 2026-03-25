using Fusion;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [Networked] private Vector3 Direction { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private int Damage { get; set; }

    private GameObject owner;

    public void Initialize(GameObject caster, int damage, float speed, Vector3 dir)
    {
        owner = caster;
        Damage = damage;
        Speed = speed;
        Direction = dir.normalized;
    }
    public override void Spawned()
    {
            Debug.Log($"SPAWNED en player: {Runner.LocalPlayer}");
    }
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        transform.position += Direction * Speed * Runner.DeltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        //if (!Object.HasStateAuthority) return;

            //evitar pegarse a sí mismo
        //if (other.gameObject == owner) return;

        //var stats = other.GetComponent<CharacterStats>();

        //if (stats != null)
       // {
        //    //stats.TakeDamage(Damage);
         //   Debug.Log($"[SERVER] Hit a {other.name} for {Damage}");
        //}

        //Runner.Despawn(Object);
    }
}