using UnityEngine;
using Fusion;

/// <summary>
/// Gestiona el HP del boss.
/// Expone HealthPercent (0-1) para que BossAI detecte el cambio de fase.
/// ResetBoss() restaura el HP y reactiva la IA para cuando todos los players mueren.
/// </summary>
public class BossHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Networked] public float CurrentHealth { get; private set; }

    [Header("References")]
    [SerializeField] private BossAI bossAI;
    [SerializeField] private ArenaRespawnManager respawnManager;

    private bool isDead = false;

    public float HealthPercent => maxHealth > 0 ? CurrentHealth / maxHealth : 0f;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            Debug.Log($"[BossHealth] HP inicial: {CurrentHealth}");
        }
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (!Object.HasStateAuthority) return;
        if (isDead) return;

        CurrentHealth -= amount;
        Debug.Log($"[BossHealth] -{amount} daño → HP: {CurrentHealth}/{maxHealth}");

        if (bossAI != null && attacker != null)
            bossAI.AddAggro(attacker.transform, amount);

        if (CurrentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[BossHealth] Boss muerto");
            if (respawnManager != null)
            respawnManager.DeactivateArena();


        if (bossAI != null)
            bossAI.DisableBoss();
        Runner.Despawn(Object);
        // Notificamos al manager que el boss murió para desactivar la arena

    }

    /// <summary>
    /// Restaura el HP y reactiva el boss. Llamado por ArenaRespawnManager
    /// cuando todos los players mueren.
    /// </summary>
    public void ResetBoss()
    {
        if (!Object.HasStateAuthority) return;

        isDead = false;
        CurrentHealth = maxHealth;

        if (bossAI != null)
            bossAI.ResetBoss();

        Debug.Log("[BossHealth] Boss reseteado");
    }
}