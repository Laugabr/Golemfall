using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Handles all UI updates, HUD elements, windows, damage, etc.
/// Local-only for now.
/// </summary>
public class UIManager : MonoBehaviour
{

    public static UIManager Instance { get; private set; }

    [Header("Health Bar")]
    public Slider HealthBar;

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

    public void SetMaxHealth(int maxHealth)
    {
        if (HealthBar == null) return;

        HealthBar.maxValue = maxHealth;
        HealthBar.value = maxHealth;
    }

    public void UpdateHealth(int currentHealth)
    {
        if (HealthBar == null) return;

        HealthBar.value = currentHealth;
    }
}