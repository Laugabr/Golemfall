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

        var bossHealth = target.GetComponent<BossHealth>();

        if (bossHealth != null)
        {
            bossHealth.TakeDamage(damage, attacker);
        }
    }
}