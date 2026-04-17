using Fusion;
using UnityEngine;

public class SimpleProjectile : NetworkBehaviour
{
    public float speed = 5f;

    public override void FixedUpdateNetwork()
    {
        transform.position += transform.forward * speed * Runner.DeltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[Projectile] Impacto con " + other.name);
    }
}
