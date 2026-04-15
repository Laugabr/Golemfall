using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

/// <summary>
/// Guarda la vida del jugador en Cloud Save.
/// - Guardado manual con botón
/// - Autoguardado cada 60 segundos
/// - Guarda automáticamente al hacer logout
/// - Carga datos al iniciar la partida
/// </summary>
public class CloudSaveGame : MonoBehaviour
{
    public static CloudSaveGame Instance { get; private set; }

    [Header("Autoguardado")]
    [SerializeField] private float autoSaveInterval = 60f;
    private float autoSaveTimer;
    private bool gameStarted = false;

    private const string KEY_HEALTH = "game_health";

    /// <summary>GameHUD escucha este evento para mostrar el estado del guardado.</summary>
    public System.Action<string> OnSaveStatusChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        if (!gameStarted) return;

        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            _ = SaveGameData();
        }
    }

    /// <summary>
    /// Llamar cuando el jugador ya spawneó en la red.
    /// Carga datos guardados y activa el autoguardado.
    /// </summary>
    public async void StartGameSave()
    {
        gameStarted = true;
        await LoadGameData();
    }

    // ── GUARDAR ──────────────────────────────────

    public async void OnSaveButton()
    {
        await SaveGameData();
    }

    public async Task SaveGameData()
    {
        try
        {
            OnSaveStatusChanged?.Invoke("Guardando...");

            int health = GetPlayerHealth();

            if (health < 0)
            {
                OnSaveStatusChanged?.Invoke("Jugador no encontrado.");
                return;
            }

            var data = new Dictionary<string, object>
            {
                { KEY_HEALTH, health }
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);

            OnSaveStatusChanged?.Invoke($"Guardado — Vida: {health}");
            Debug.Log($"[CloudSaveGame] Guardado: Vida={health}");
        }
        catch (CloudSaveException e)
        {
            OnSaveStatusChanged?.Invoke("Error al guardar.");
            Debug.LogError("[CloudSaveGame] Error: " + e.Message);
        }
    }

    // ── CARGAR ────────────────────────────────────

    public async Task LoadGameData()
    {
        try
        {
            OnSaveStatusChanged?.Invoke("Cargando partida...");

            var keys = new HashSet<string> { KEY_HEALTH };
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (results.TryGetValue(KEY_HEALTH, out var healthItem))
            {
                int health = healthItem.Value.GetAs<int>();
                ApplyPlayerHealth(health);
                OnSaveStatusChanged?.Invoke($"Partida cargada — Vida: {health}");
                Debug.Log($"[CloudSaveGame] Cargado: Vida={health}");
            }
            else
            {
                OnSaveStatusChanged?.Invoke("Partida nueva.");
                Debug.Log("[CloudSaveGame] Sin datos previos de vida.");
            }
        }
        catch (CloudSaveException e)
        {
            OnSaveStatusChanged?.Invoke("Partida nueva.");
            Debug.Log("[CloudSaveGame] Sin datos previos: " + e.Message);
        }
    }

    // ── HELPERS ───────────────────────────────────

    private int GetPlayerHealth()
    {
        var healthSystems = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (var hs in healthSystems)
        {
            if (hs.Object != null && hs.Object.HasInputAuthority)
                return hs.CurrentHealth;
        }
        return -1;
    }

    private void ApplyPlayerHealth(int health)
    {
        var healthSystems = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (var hs in healthSystems)
        {
            if (hs.Object != null && hs.Object.HasStateAuthority)
            {
                hs.CurrentHealth = Mathf.Min(health, hs.MaxHealth);
                Debug.Log($"[CloudSaveGame] Vida aplicada: {hs.CurrentHealth}");
            }
        }
    }
}