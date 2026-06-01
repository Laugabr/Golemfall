using UnityEngine;

public class EnemyHealth : HealthSystem
{
    [SerializeField] private EnemyHealthBar healthBar;

    public override void Spawned()
    {
        var charStats = GetComponent<CharacterStats>();
        if (charStats != null)
            charStats.Initialize();

        base.Spawned();

        if (Object.HasStateAuthority)
        {
            RecalculateMaxHealth();
            CurrentHealth = MaxHealth;
            healthBar?.SetMaxHealth(MaxHealth);
            healthBar?.SetHealth(CurrentHealth);
        }
    }

    public override void Render()
    {
        healthBar?.SetMaxHealth(MaxHealth);
        healthBar?.SetHealth(CurrentHealth);
    }

    public override void CurrentHealthChanged()
    {
        base.CurrentHealthChanged();
        healthBar?.SetHealth(CurrentHealth);
    }

    public override void MaxHealthChanged()
    {
        base.MaxHealthChanged();
        healthBar?.SetMaxHealth(MaxHealth);
        healthBar?.SetHealth(CurrentHealth);
    }

    private void RecalculateMaxHealth()
    {
        if (!Object.HasStateAuthority) return;

        int newMax = stats.GetStat(Stat.maxHealth);
        MaxHealth = newMax;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
    }

    public override int GetArmor()
    {
        if (stats != null)
            return stats.GetStat(Stat.armor);

        return 0;
    }

    public override void Die()
    {
        Debug.Log($"Enemy {gameObject.name} murió");

        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}