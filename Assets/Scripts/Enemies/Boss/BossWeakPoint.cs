using UnityEngine;

public class BossWeakPoint : MonoBehaviour
{
    [SerializeField] private BossHealth bossHealth;

    public void TakeDamage(float amount, Transform attacker)
    {
        Debug.Log("[WeakPoint] Damage recibido");

        bossHealth.TakeDamage(amount, attacker);
    }
}