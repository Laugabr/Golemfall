using UnityEngine;

public class PlayerHealth : HealthSystem
{
    public override void Spawned()
    {
        base.Spawned();

        if (!Object.HasStateAuthority) return;

        // Suscribirse a cambios de stats
        if (stats is PlayerStats playerStats)
            playerStats.OnStatsChanged.AddListener(RecalculateMaxHealth);

        RecalculateMaxHealth();
        CurrentHealth = MaxHealth;
    }

    private void OnDestroy()
    {
        if (stats != null && stats is PlayerStats playerStats)
            playerStats.OnStatsChanged.RemoveListener(RecalculateMaxHealth);
    }

    public void RecalculateMaxHealth()
    {
        if (!Object.HasStateAuthority) return;

        int newMax = stats.GetStat(Stat.maxHealth);
        MaxHealth = newMax;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
    }

    public override int GetArmor()
    {
        return stats.GetStat(Stat.armor);
    }

    public override void MaxHealthChanged()
    {
        Debug.Log($"MaxHealth changed to {MaxHealth}");
    }

    public override void CurrentHealthChanged()
    {
        Debug.Log($"CurrentHealth changed to {CurrentHealth}");
    }

    public override void Die()
    {
        Debug.Log($"Player {gameObject.name} ha muerto.");
    }
}