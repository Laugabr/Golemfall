using UnityEngine;
using Fusion;

public class DealDamage : NetworkBehaviour
{
    [SerializeField] private float damage = 10f;

    private Transform attacker;

    public void SetAttacker(Transform atk)
    {
        attacker = atk;
    }

    public void ApplyDamage(GameObject target)
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log($"[Damage] {attacker?.name} golpea a {target.name}");

        // =============================
        //  WEAK POINT 
        // =============================
        var weakPoint = target.GetComponent<BossWeakPoint>();
        if (weakPoint != null)
        {
            Debug.Log("[Damage] Hit WeakPoint");

            weakPoint.TakeDamage(damage, attacker);
            return;
        }

        // =============================
        //  BOSS DIRECTO 
        // =============================
        var bossHealth = target.GetComponent<BossHealth>();
        if (bossHealth != null)
        {
            Debug.Log("[Damage] Hit Boss directamente");

            bossHealth.TakeDamage(damage, attacker);
            return;
        }
    }
}