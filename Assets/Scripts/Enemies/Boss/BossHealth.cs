using UnityEngine;
using Fusion;

public class BossHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Networked] private float currentHealth { get; set; }

    [Header("References")]
    [SerializeField] private BossAI bossAI;

    private bool isDead = false;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            currentHealth = maxHealth;
            Debug.Log($"[BossHealth] HP inicial: {currentHealth}");
        }
    }

    public void TakeDamage(float amount, Transform attacker)
    {
        if (!Object.HasStateAuthority) return;
        if (isDead) return;

        currentHealth -= amount;

        Debug.Log($"[BossHealth] Recibe {amount} daño → HP: {currentHealth}");

        //  AGGRO
        if (bossAI != null && attacker != null)
        {
            bossAI.AddAggro(attacker, amount);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log("[BossHealth] Boss muerto");

        //  detener IA
        if (bossAI != null)
        {
            bossAI.DisableBoss();
        }

        
    }
}