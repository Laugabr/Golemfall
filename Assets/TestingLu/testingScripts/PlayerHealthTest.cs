using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<int> OnHealthChanged; // útil si querés actualizar UI

    [SerializeField] MeshRenderer meshRenderer;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (meshRenderer != null)
        StartCoroutine(ChangeColor());
        

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            TakeDamage(5);
        }
    }
    IEnumerator ChangeColor()
    {
        
        var currentColor = meshRenderer.material.GetColor("_BaseColor");
        meshRenderer.material.SetColor("_BaseColor", Color.red);
        yield return new WaitForSeconds(0.2f);
        meshRenderer.material.SetColor("_BaseColor", currentColor);

    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    private void Die()
    {
        BasicEventsManager.OnExperienceGain?.Invoke(100);

        OnDeath?.Invoke();
        Destroy(gameObject); // Por ahora destruimos el objeto
    }
}