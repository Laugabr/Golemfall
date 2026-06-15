using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

public class CloudSaveGame : MonoBehaviour
{
    public static CloudSaveGame Instance { get; private set; }

    [Header("Auto Save")]
    [SerializeField] private bool enableAutoSave = false; // EXPO: apagado. Reactivar post-expo.
    [SerializeField] private float autoSaveInterval = 60f;
    private float autoSaveTimer;
    private bool gameStarted = false;

    private const string KEY_HEALTH     = "game_health";
    private const string KEY_EXPERIENCE = "game_experience";

    // Pending values to apply once the player spawns
    private int pendingHealth     = -1;
    private int pendingExperience = -1;

    /// <summary>GameHUD listens to this event to show save status.</summary>
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

        // Apply pending values each frame until the player is available
        if (pendingHealth > 0)
            TryApplyPendingHealth();

        if (pendingExperience >= 0)
            TryApplyPendingExperience();

        // Periodic auto save
        if (enableAutoSave)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                autoSaveTimer = 0f;
                _ = SaveGameData();
            }
        }
    }

    /// <summary>
    /// Call once the local player has spawned in the network.
    /// Loads saved data and activates auto save.
    /// </summary>
    public async void StartGameSave()
    {
        gameStarted = true;
        await LoadGameData();
    }

    // SAVE 

    public async void OnSaveButton()
    {
        await SaveGameData();
    }

    public async Task SaveGameData()
    {
        try
        {
            OnSaveStatusChanged?.Invoke("Saving...");

            int health = GetPlayerHealth();
            if (health < 0)
            {
                OnSaveStatusChanged?.Invoke("Player not found.");
                return;
            }

            int experience = GetPlayerExperience();

            var data = new Dictionary<string, object>
            {
                { KEY_HEALTH,     health },
                { KEY_EXPERIENCE, experience }
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);

            OnSaveStatusChanged?.Invoke($"Saved — HP: {health} | XP: {experience}");
            Debug.Log($"[CloudSaveGame] Saved: Health={health}, Experience={experience}");
        }
        catch (CloudSaveException e)
        {
            OnSaveStatusChanged?.Invoke("Save error.");
            Debug.LogError("[CloudSaveGame] Save error: " + e.Message);
        }
    }

    // LOAD

    public async Task LoadGameData()
    {
        try
        {
            OnSaveStatusChanged?.Invoke("Loading save...");

            var keys    = new HashSet<string> { KEY_HEALTH, KEY_EXPERIENCE };
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            // Health
            if (results.TryGetValue(KEY_HEALTH, out var healthItem))
            {
                int health = healthItem.Value.GetAs<int>();
                Debug.Log($"[CloudSaveGame] Loaded: Health={health}");

                if (!TryApplyHealth(health))
                {
                    pendingHealth = health;
                    Debug.Log("[CloudSaveGame] PlayerHealth not ready yet, queuing...");
                    OnSaveStatusChanged?.Invoke($"Loaded — HP: {health} (applying...)");
                }
                else
                {
                    OnSaveStatusChanged?.Invoke($"Save loaded — HP: {health}");
                }
            }
            else
            {
                Debug.Log("[CloudSaveGame] No previous health data.");
            }

            // Experience
            if (results.TryGetValue(KEY_EXPERIENCE, out var xpItem))
            {
                int experience = xpItem.Value.GetAs<int>();
                Debug.Log($"[CloudSaveGame] Loaded: Experience={experience}");

                if (!TryApplyExperience(experience))
                {
                    pendingExperience = experience;
                    Debug.Log("[CloudSaveGame] ExperienceManager not ready yet, queuing...");
                }
                else
                {
                    OnSaveStatusChanged?.Invoke($"Save loaded — HP: {GetPlayerHealth()} | XP: {experience}");
                }
            }
            else
            {
                Debug.Log("[CloudSaveGame] No previous experience data.");
                OnSaveStatusChanged?.Invoke("New game.");
            }
        }
        catch (CloudSaveException e)
        {
            OnSaveStatusChanged?.Invoke("New game.");
            Debug.Log("[CloudSaveGame] No previous save: " + e.Message);
        }
    }

    // APPLY HEALTH 

    /// <summary>Tries to apply health to the local player. Returns true if successful.</summary>
    private bool TryApplyHealth(int health)
    {
        var healthSystems = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (var hs in healthSystems)
        {
            if (hs.Object != null && hs.Object.HasStateAuthority)
            {
                hs.CurrentHealth = Mathf.Min(health, hs.MaxHealth);
                Debug.Log($"[CloudSaveGame] Health applied: {hs.CurrentHealth}/{hs.MaxHealth}");
                return true;
            }
        }
        return false;
    }

    private void TryApplyPendingHealth()
    {
        if (TryApplyHealth(pendingHealth))
        {
            OnSaveStatusChanged?.Invoke($"Save loaded — HP: {pendingHealth}");
            pendingHealth = -1;
        }
    }

    // APPLY EXPERIENCE 

    /// <summary>Tries to apply experience to the local player. Returns true if successful.</summary>
    private bool TryApplyExperience(int experience)
    {
        var expManagers = FindObjectsByType<ExperienceManager>(FindObjectsSortMode.None);
        foreach (var em in expManagers)
        {
            if (em.Object != null && em.Object.HasStateAuthority)
            {
                em.AddExperience(experience);
                Debug.Log($"[CloudSaveGame] Experience applied: {experience}");
                return true;
            }
        }
        return false;
    }

    private void TryApplyPendingExperience()
    {
        if (TryApplyExperience(pendingExperience))
        {
            OnSaveStatusChanged?.Invoke($"Save loaded — XP: {pendingExperience}");
            pendingExperience = -1;
        }
    }

    // HELPERS

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

    private int GetPlayerExperience()
    {
        var expManagers = FindObjectsByType<ExperienceManager>(FindObjectsSortMode.None);
        foreach (var em in expManagers)
        {
            if (em.Object != null && em.Object.HasInputAuthority)
                return em.TotalExperience;
        }
        return 0;
    }
}