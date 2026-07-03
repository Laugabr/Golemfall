using System;
using Fusion;
using Unity.Mathematics;
using UnityEngine;

public class HealthSystem : NetworkBehaviour, IDamageable
{

    [SerializeField] protected CharacterStats stats;

    [Networked, OnChangedRender(nameof(OnMaxHealthChanged))]
    public int MaxHealth { get; set; }

    [Networked, OnChangedRender(nameof(OnCurrentHealthChanged))]
    public int CurrentHealth { get; set; }


    [SerializeField] private int localCurrentHealth;
    [SerializeField] private int localMaxHealth;


    public override void Spawned()
    {
        if(!Object.HasStateAuthority) return;
        stats = GetComponent<CharacterStats>();
        if(stats == null)
        {
            Debug.LogError($"HealthSystem requires CharacterStats on {gameObject.name}");
        }
        else
        {
            MaxHealth = stats.GetStat(Stat.maxHealth);
            CurrentHealth = MaxHealth;
            localMaxHealth = MaxHealth;
        }

    }

    public virtual void MaxHealthChanged()
    {
       
    }
    public virtual void CurrentHealthChanged()
    {
        localCurrentHealth = CurrentHealth;

    }


    private void OnMaxHealthChanged()
    {
        localCurrentHealth = MaxHealth;
        MaxHealthChanged();
    }

    private void OnCurrentHealthChanged()
    {
        
        localCurrentHealth = CurrentHealth;

        CurrentHealthChanged();
    }
    public virtual void TakeDamage(int amount, GameObject source)
    {
        if (!Object.HasStateAuthority) return;

        int armor = GetArmor();

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(
            amount * (100f / (100f + armor))
        ));

        CurrentHealth -= finalDamage;


        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    public virtual void Heal(int amount)
    {
        if (!Object.HasStateAuthority) return;
    
        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth)
            CurrentHealth = MaxHealth;
    }

    public virtual int GetArmor() //Tries to get armor, but if not player stats, returns 0
    {
        if (stats != null)
            return stats.GetStat(Stat.armor);

        return 0;
    }
    public virtual void Die()
    {
        Debug.Log($"{gameObject.name} murió");
    }

    }