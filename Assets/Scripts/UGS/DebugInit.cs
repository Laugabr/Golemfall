using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

/// <summary>
/// SOLO PARA TESTING — Permite probar Integration sin pasar por AuthenticationScene.
/// Inicializa Unity Services y hace login anónimo automáticamente.
/// 
/// SETUP:
/// 1. Crear un GameObject "DebugInit" en Integration
/// 2. Agregarle este script
/// 3. DESACTIVAR el GameObject cuando pruebes el flujo completo (Auth → Integration)
/// 4. BORRAR antes del build final
/// </summary>
public class DebugInit : MonoBehaviour
{
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
    }
}