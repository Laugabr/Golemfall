using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LogOutManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button logoutButton;

    [Header("Config")]
    [SerializeField] private string sceneToSignIn = "AuthenticationScene";

    public async void OnLogOut()
    {
        // 1. Guardar datos del juego antes de salir
        if (CloudSaveGame.Instance != null)
        {
            Debug.Log("[LogOut] Guardando datos antes de cerrar sesión...");
            await CloudSaveGame.Instance.SaveGameData();
        }

        // 2. Disparar evento de logout
        if (GameEvents.instance != null)
            GameEvents.instance.TriggerLogout();

        // 3. Cerrar sesión
        AuthenticationService.Instance.SignOut();
        Debug.Log("[LogOut] Sesión cerrada.");

        // 4. Volver a la pantalla de login
        SceneManager.LoadScene(sceneToSignIn);
    }
}