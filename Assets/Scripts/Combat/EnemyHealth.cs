using UnityEngine;

public class EnemyHealth : HealthSystem
{

    public override void Spawned()
    {
        base.Spawned();

        if (Object.HasStateAuthority)
        {
            RecalculateMaxHealth();
            CurrentHealth = MaxHealth;
        }


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