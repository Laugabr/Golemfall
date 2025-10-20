using UnityEngine;
using Fusion;
using System;

public class CombatManager : NetworkBehaviour
{
    public static CombatManager Instance { get; private set; }

    public event Action<int, int> OnHealthChanged;
    // Params: (playerId, newHealth)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    // Called by clients when they want to deal damage
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestDamage(int targetId, int damage)
    {
        // Server authority validates
        ApplyDamage(targetId, damage);
    }

    private void ApplyDamage(int targetId, int damage)
    {
        // TODO: Lookup PlayerHealth by id
        PlayerHealth target = FindPlayerById(targetId);
        if (target == null) return;

        int newHealth = target.CurrentHealth - damage;
        target.SetHealth(newHealth);

        // Notify everyone of health change
        RPC_SyncHealth(targetId, newHealth);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SyncHealth(int targetId, int newHealth)
    {
        PlayerHealth target = FindPlayerById(targetId);
        if (target == null) return;

        target.SetHealth(newHealth);

        OnHealthChanged?.Invoke(targetId, newHealth);
    }

    private PlayerHealth FindPlayerById(int playerId)
    {
        // TODO: Implement lookup (Dictionary<int, PlayerHealth>)
        return null;
    }
}
