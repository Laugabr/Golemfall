using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class BurnArea : NetworkBehaviour
{
    [SerializeField] private float duration = 4f;
    [SerializeField] private float tickRate = 1f;

    private float spawnTime;
    private float lastTick;

    private DealDamage damage;
    [SerializeField] private int damageAmount = 5;

    private HashSet<GameObject> targetsInside = new();

    private void Awake()
    {
        damage = GetComponent<DealDamage>();
    }

    public override void Spawned()
    {
        spawnTime = Time.time;
        Debug.Log("[BurnArea] Spawned");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        //  destruir después de duración
        if (Time.time >= spawnTime + duration)
        {
            Debug.Log("[BurnArea] Destruido");
            Runner.Despawn(Object);
            return;
        }

        //  aplicar daño por ticks
        if (Time.time >= lastTick + tickRate)
        {
            lastTick = Time.time;

            foreach (var target in targetsInside)
            {
                if (target == null) continue;

                Debug.Log("[BurnArea] Tick daño a " + target.name);
                target.GetComponent<IDamageable>()?.TakeDamage(damageAmount, gameObject); // Aplica daño al player
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        targetsInside.Add(other.gameObject);
        Debug.Log("[BurnArea] Entra: " + other.name);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        targetsInside.Remove(other.gameObject);
        Debug.Log("[BurnArea] Sale: " + other.name);
    }
}
