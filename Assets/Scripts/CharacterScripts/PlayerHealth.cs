using UnityEngine;

/// <summary>
/// Passive health container. Does not perform network communication.
/// Host is expected to be authoritative: CombatManager will call SetHealth on host
/// and the host will call RPC_SyncHealth to propagate the value to clients.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    public int MaxHealth => maxHealth;

    public int CurrentHealth { get; private set; }

    // Optional stable player id assigned externally (used for lookups in CombatManager)
    public int PlayerId { get; private set; } = -1;

    void Start()
    {
        CurrentHealth = maxHealth;

        // Initialize UI for local player if present
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMaxHealth(maxHealth);
            UIManager.Instance.UpdateHealth(CurrentHealth);
        }
    }

    /// <summary>
    /// Assign a stable player id used by CombatManager
    /// Call this when the player is created / registered.
    /// </summary>
    public void SetPlayerId(int id)
    {
        PlayerId = id;
    }

    /// <summary>
    /// Set health to an authoritative value (called by host or after sync).
    /// This method also updates local UI and triggers player death event when necessary.
    /// </summary>
    public void SetHealth(int value)
    {
        CurrentHealth = Mathf.Clamp(value, 0, maxHealth);

        // Update UI locally (safe to call on clients and host)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(CurrentHealth);
        }

        if (CurrentHealth <= 0)
        {
            // Inform PlayerManager of death (PlayerManager remains local dispatcher)
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.InvokePlayerDeath();
        }
    }
}
