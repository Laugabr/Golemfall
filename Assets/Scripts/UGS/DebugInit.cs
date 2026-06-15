using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

/// <summary>
/// SOLO PARA TESTING — Permite probar Integration sin pasar por AuthenticationScene.
/// Inicializa Unity Services y hace login anónimo automáticamente.
/// Mantiene el mismo ID anónimo, pero borra el progreso guardado (vida y
/// experiencia) en cada arranque para empezar siempre de cero.
/// </summary>
public class DebugInit : MonoBehaviour
{
    [Header("Testing")]
    [Tooltip("Borra vida y experiencia del cloud save al iniciar, para arrancar de cero.")]
    [SerializeField] private bool clearProgressOnStart = true;

    // Deben coincidir con las claves usadas en CloudSaveGame.
    private const string KEY_HEALTH     = "game_health";
    private const string KEY_EXPERIENCE = "game_experience";

    async void Awake()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            Debug.Log("[DebugInit] Inicializando Unity Services...");
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("[DebugInit] Login anónimo para testing...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("[DebugInit] OK. Player ID: " + AuthenticationService.Instance.PlayerId);
        }
        else
        {
            Debug.Log("[DebugInit] Ya logueado. Player ID: " + AuthenticationService.Instance.PlayerId);
        }

        // Empezar de cero manteniendo el mismo ID
        if (clearProgressOnStart)
            await ClearSavedProgress();
    }

    private async Task ClearSavedProgress()
    {
        await TryDeleteKey(KEY_HEALTH);
        await TryDeleteKey(KEY_EXPERIENCE);
        Debug.Log("[DebugInit] Progreso reiniciado (vida y experiencia borradas).");
    }

    private async Task TryDeleteKey(string key)
    {
        try
        {
            await CloudSaveService.Instance.Data.Player.DeleteAsync(key);
        }
        catch (CloudSaveException e)
        {
            Debug.Log($"[DebugInit] '{key}' sin datos que borrar: {e.Message}");
        }
    }
}