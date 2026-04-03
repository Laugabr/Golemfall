using UnityEngine;
using Fusion;

public class DestructibleObject : NetworkBehaviour, IDamageable
{
    public void TakeDamage(int amount, GameObject source)
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log($"{gameObject.name} destruido por {source.name}");

        Runner.Despawn(Object);
    }
}