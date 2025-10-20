using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    public int MaxHealth => maxHealth; // Read-only property

    public int CurrentHealth { get; private set; }

    void Start()
    {
        CurrentHealth = maxHealth;
        UIManager.Instance.SetMaxHealth(maxHealth);
        UIManager.Instance.UpdateHealth(CurrentHealth);
    }

    public void SetHealth(int value)
    {
        CurrentHealth = Mathf.Clamp(value, 0, maxHealth);
        UIManager.Instance.UpdateHealth(CurrentHealth);

        if (CurrentHealth <= 0)
            PlayerManager.Instance.InvokePlayerDeath();
    }
}
