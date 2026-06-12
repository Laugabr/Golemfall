using UnityEngine;
using Fusion;

/// <summary>
/// Gestiona el HP del boss.
/// Expone HealthPercent (0–1) para que BossAI detecte el cambio de fase.
/// Solo el host modifica los valores; currentHealth está networkeado para que
/// las barras de UI en clientes puedan leerlo.
/// </summary>
public class BossHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Networked] public float CurrentHealth { get; private set; }

    [Header("References")]
    [SerializeField] private BossAI bossAI;

    private bool isDead = false;

    /// <summary>Porcentaje de vida restante entre 0 y 1.</summary>
    public float HealthPercent => maxHealth > 0 ? CurrentHealth / maxHealth : 0f;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            Debug.Log($"[BossHealth] HP inicial: {CurrentHealth}");
        }
    }

    public void TakeDamage(float amount, Transform attacker)
    {
        if (!Object.HasStateAuthority) return;
        if (isDead) return;

        CurrentHealth -= amount;
        Debug.Log($"[BossHealth] -{amount} daño → HP: {CurrentHealth}/{maxHealth}");

        if (bossAI != null && attacker != null)
            bossAI.AddAggro(attacker, amount);

        if (CurrentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[BossHealth] Boss muerto");

        if (bossAI != null)
            bossAI.DisableBoss();
    }
}