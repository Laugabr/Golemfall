using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using TMPro;

public class AuthManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text togglePasswordText;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        statusText.text = "Listo para iniciar sesión.";
        togglePasswordText.text = "(-)";
    }

    public async void OnRegisterButton()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        try
        {
            await AuthenticationService.Instance
                  .SignUpWithUsernamePasswordAsync(username, password);

            statusText.text = "Registro exitoso! Player ID: "
                              + AuthenticationService.Instance.PlayerId;
        }
        catch (AuthenticationException e)
        {
            statusText.text = "Error: " + e.Message;
        }
        catch (RequestFailedException e)
        {
            statusText.text = "Error: " + e.Message;
        }
    }

    public async void OnLoginButton()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        try
        {
            await AuthenticationService.Instance
                  .SignInWithUsernamePasswordAsync(username, password);

            statusText.text = "Login exitoso! Player ID: "
                              + AuthenticationService.Instance.PlayerId;
        }
        catch (AuthenticationException e)
        {
            statusText.text = "Error: " + e.Message;
        }
        catch (RequestFailedException e)
        {
            statusText.text = "Error: " + e.Message;
        }
    }

    public void OnLogoutButton()
    {
        AuthenticationService.Instance.SignOut();
        statusText.text = "Sesión cerrada.";
        // Limpia los campos de texto al cerrar sesión
        usernameInput.text = "";
        passwordInput.text = "";
    }

    private bool passwordVisible = false;

    public void OnTogglePasswordButton()
    {
        passwordVisible = !passwordVisible;

        if (passwordVisible)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Standard;
            passwordInput.textComponent.text = passwordInput.text;
            togglePasswordText.text = "(o)";
        }
        else
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            togglePasswordText.text = "(-)";
        }

        passwordInput.ForceLabelUpdate();
    }

    // Inicia sesión de forma anónima, sin usuario ni contraseña
    // El sistema genera un Player ID automáticamente
    public async void OnAnonymousLoginButton()
    {
        // Si ya hay una sesión activa, avisamos y salimos
        if (AuthenticationService.Instance.IsSignedIn)
        {
            statusText.text = "Ya hay una sesión activa. Hacé Logout primero.";
            return;
        }

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            statusText.text = "Login anónimo exitoso! Player ID: "
                              + AuthenticationService.Instance.PlayerId;
        }
        catch (AuthenticationException e)
        {
            statusText.text = "Error: " + e.Message;
        }
        catch (RequestFailedException e)
        {
            statusText.text = "Error: " + e.Message;
        }
    }
}