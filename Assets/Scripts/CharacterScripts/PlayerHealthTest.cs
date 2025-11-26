using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterStats))]
public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int localMaxHealth;
    [SerializeField] private int currentHealth;

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<int> OnHealthChanged; // útil si querés actualizar UI

    [SerializeField] private CharacterStats charStats;

    [SerializeField] MeshRenderer meshRenderer;


    private void OnEnable()
    {
        charStats = GetComponent<CharacterStats>();
        if (charStats != null && charStats is PlayerStats player)
        {
            player.OnStatsChanged.AddListener(UpdateMaxHealth);
        }
    }

    private void OnDisable()
    {
        if (charStats != null && charStats is PlayerStats player)
        {
            player.OnStatsChanged.RemoveListener(UpdateMaxHealth);
        }
    }

    private void UpdateMaxHealth()
    {
        int newMaxHealth = charStats.GetStat(Stat.maxHealth);
        
        currentHealth = Mathf.Clamp(currentHealth, 0, newMaxHealth);
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"Vida máxima actualizada: {newMaxHealth}");
    }
    void Start()
    {
        if (charStats == null)
        {
            Debug.LogError("character stats component not found in " + gameObject.name);
        }

    }
    public void TakeDamage(int amount)
    {
        localMaxHealth = charStats.GetStat(Stat.maxHealth); 

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, localMaxHealth);

        if (meshRenderer != null)
            StartCoroutine(ChangeColor());

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void Update()
    {
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
        localMaxHealth = charStats.GetStat(Stat.maxHealth);
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, localMaxHealth);
        OnHealthChanged?.Invoke(currentHealth);

    }   

    public virtual void Die()
    {
        BasicEventsManager.OnExperienceGain?.Invoke(100);

        OnDeath?.Invoke();
        Destroy(gameObject); // Por ahora destruimos el objeto
    }
}