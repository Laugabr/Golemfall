using UnityEngine;
using Fusion;

public class BossHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Networked] private float currentHealth { get; set; }

    private BossAI bossAI;

    private void Awake()
    {
        bossAI = GetComponent<BossAI>();
    }

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

        currentHealth -= amount;

        Debug.Log($"[BossHealth] Recibe {amount} daño → HP: {currentHealth}");

        //  AGGRO AQUI
        if (bossAI != null && attacker != null)
        {
            bossAI.AddAggro(attacker, amount);
            Debug.Log($"[BossHealth] Agregando aggro a {attacker.name}");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("[BossHealth] Boss muerto");

        // lógica de muerte futura
    }
}
