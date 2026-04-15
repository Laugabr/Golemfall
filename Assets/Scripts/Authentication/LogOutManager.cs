using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Unity.Services.Core;

public class LogOutManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button logoutButton;

    [Header("Config")]
    [SerializeField] private string sceneToSignIn = "AuthenticationScene";

    public void OnLogOut()
    {
        // Disparar evento de logout para que sistemas interesados escuchen (ej resourcemanager)
        if (GameEvents.instance != null)
        {
            GameEvents.instance.TriggerLogout();
        }

        AuthenticationService.Instance.SignOut();
        Debug.Log("Log Out Player");

        SceneManager.LoadScene(sceneToSignIn);
    }
}