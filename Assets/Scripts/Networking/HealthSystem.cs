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


    private int localMaxHealth;

    public override void Spawned()
    {
        localMaxHealth = MaxHealth;
    }

    public virtual void MaxHealthChanged()
    {
       
    }
    public virtual void CurrentHealthChanged()
    {
        
    }


    private void OnMaxHealthChanged()
    {
        localMaxHealth = MaxHealth;
        MaxHealthChanged();
    }

    private void OnCurrentHealthChanged()
    {
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

        Debug.Log($"[SERVER] {gameObject.name} recibió {finalDamage} daño");

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
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