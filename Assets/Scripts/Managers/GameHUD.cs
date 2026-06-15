using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Barra de Vida")]
    [SerializeField] private Image healthFill;
    [SerializeField] private HoverDetailLabel healthLabel;

    [Header("Guardado")]
    [SerializeField] private TMP_Text saveStatusText;

    [Header("Test de daño")]
    [SerializeField] private int testDamageAmount = 5;

    private PlayerHealth playerHealth;

    private void Start()
    {
        // Escuchar estado de guardado
        if (CloudSaveGame.Instance != null)
            CloudSaveGame.Instance.OnSaveStatusChanged += UpdateSaveStatus;

        if (saveStatusText != null)
            saveStatusText.text = "";
    }

    private void OnDestroy()
    {
        if (CloudSaveGame.Instance != null)
            CloudSaveGame.Instance.OnSaveStatusChanged -= UpdateSaveStatus;
    }

    private void Update()
    {
        // Buscar el PlayerHealth del jugador local si aún no lo tenemos
        if (playerHealth == null)
        {
            var healthSystems = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            foreach (var hs in healthSystems)
            {
                if (hs.Object != null && hs.Object.HasInputAuthority)
                {
                    playerHealth = hs;
                    Debug.Log($"[GameHUD] PlayerHealth encontrado: {hs.CurrentHealth}/{hs.MaxHealth}");
                    break;
                }
            }
        }

        // Actualizar barra de vida cada frame
        UpdateHealthBar();

        // Tecla Ñ para dañarse (testing)
        if (Input.GetKeyDown(KeyCode.Semicolon) && playerHealth != null)
        {
            // KeyCode.Semicolon corresponde a la tecla Ñ en teclado latinoamericano
            playerHealth.TakeDamage(testDamageAmount, playerHealth.gameObject);
            Debug.Log($"[GameHUD] ¡Te hiciste {testDamageAmount} de daño! Vida: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
        }
    }

    // ── VIDA ─────────────────────────────────────

    private void UpdateHealthBar()
    {
        if (playerHealth == null) return;

        int current = playerHealth.CurrentHealth;
        int max = playerHealth.MaxHealth;

        if (healthFill != null && max > 0)
            healthFill.fillAmount = (float)current / max;

        if (healthLabel != null)
        {
            // Por ahora la vida NO muestra ningún número, solo la barra.
            healthLabel.Set("", "");

            // Para reactivar el número (y el detalle "115 / 150" en hover),
            // comentar la línea de arriba y descomentar esta:
            // healthLabel.Set(current.ToString(), $"{current} / {max}");
        }
    }

    // ── GUARDADO ─────────────────────────────────

    /// <summary>Conectar al OnClick del botón "Guardar".</summary>
    public void OnSaveButton()
    {
        if (CloudSaveGame.Instance != null)
            CloudSaveGame.Instance.OnSaveButton();
        else
            Debug.LogWarning("[GameHUD] CloudSaveGame no encontrado.");
    }

    private void UpdateSaveStatus(string status)
    {
        if (saveStatusText != null)
            saveStatusText.text = status;
    }
}