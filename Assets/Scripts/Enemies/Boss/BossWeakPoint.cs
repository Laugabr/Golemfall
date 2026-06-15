using Fusion;
using UnityEngine;

public class BossWeakPoint : NetworkBehaviour, IDamageable
{
    [SerializeField] private BossHealth bossHealth;

    public void TakeDamage(int amount, GameObject source)
    {
        Debug.Log("[WeakPoint] Damage recibido");

        bossHealth.TakeDamage(amount, source);
    }
}