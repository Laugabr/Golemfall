using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

public class CloudSaveGame : MonoBehaviour
{
    public static CloudSaveGame Instance { get; private set; }

    [Header("Autoguardado")]
    [SerializeField] private float autoSaveInterval = 60f;
    private float autoSaveTimer;
    private bool gameStarted = false;

    private const string KEY_HEALTH = "game_health";

    // Vida pendiente de aplicar (cuando se carga antes de que el player exista)
    private int pendingHealth = -1;

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

        // Si hay vida pendiente de aplicar, intentar cada frame
        if (pendingHealth > 0)
            TryApplyPendingHealth();

        // Autoguardado periódico
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
                Debug.Log($"[CloudSaveGame] Datos cargados de la nube: Vida={health}");

                // Intentar aplicar inmediatamente
                if (!TryApplyHealth(health))
                {
                    // Si no se pudo (player no existe todavía), guardar como pendiente
                    pendingHealth = health;
                    Debug.Log("[CloudSaveGame] PlayerHealth no encontrado aún, esperando spawn...");
                    OnSaveStatusChanged?.Invoke($"Cargado — Vida: {health} (aplicando...)");
                }
                else
                {
                    OnSaveStatusChanged?.Invoke($"Partida cargada — Vida: {health}");
                }
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

    // ── APLICAR VIDA ─────────────────────────────

    /// <summary>Intenta aplicar la vida. Retorna true si encontró al player.</summary>
    private bool TryApplyHealth(int health)
    {
        var healthSystems = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (var hs in healthSystems)
        {
            if (hs.Object != null && hs.Object.HasStateAuthority)
            {
                hs.CurrentHealth = Mathf.Min(health, hs.MaxHealth);
                Debug.Log($"[CloudSaveGame] Vida aplicada: {hs.CurrentHealth}/{hs.MaxHealth}");
                return true;
            }
        }
        return false;
    }

    /// <summary>Llamado cada frame mientras haya vida pendiente.</summary>
    private void TryApplyPendingHealth()
    {
        if (TryApplyHealth(pendingHealth))
        {
            OnSaveStatusChanged?.Invoke($"Partida cargada — Vida: {pendingHealth}");
            pendingHealth = -1; // Ya se aplicó, no reintentar
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
}