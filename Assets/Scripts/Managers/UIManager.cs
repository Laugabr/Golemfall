using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal UI manager focused on player's health bar.
/// later extend to support multiple players (by playerId).
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Single-player health")]
    [SerializeField] private Slider healthBar; // assign in inspector

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetMaxHealth(int maxHealth)
    {
        if (healthBar == null) return;

        healthBar.maxValue = maxHealth;
        healthBar.value = maxHealth;
    }

    public void UpdateHealth(int currentHealth)
    {
        if (healthBar == null) return;

        healthBar.value = currentHealth;
    }

    // Future: overloads to support multiple players:
    // public void UpdateHealthForPlayer(int playerId, int currentHealth) { ... }
}
