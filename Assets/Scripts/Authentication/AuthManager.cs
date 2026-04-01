using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AuthManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text togglePasswordText;

    // >>> NUEVO: referencias a los botones para habilitarlos/deshabilitarlos
    [SerializeField] private Button registerButton;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button anonymousLoginButton;

    async void Start()
    {
        // >>> NUEVO: deshabilitar botones mientras inicializa (consejo del profe)
        registerButton.enabled = false;
        loginButton.enabled = false;
        anonymousLoginButton.enabled = false;

        statusText.text = "Inicializando...";
        togglePasswordText.text = "(-)";

        await UnityServices.InitializeAsync();

        // >>> NUEVO: habilitar recién cuando UGS está listo
        registerButton.enabled = true;
        loginButton.enabled = true;
        anonymousLoginButton.enabled = true;

        statusText.text = "Listo para iniciar sesión.";
        Debug.Log("UnityServices inicializado correctamente."); // >>> NUEVO
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
            Debug.Log("Registro exitoso. Player ID: " + AuthenticationService.Instance.PlayerId); // >>> NUEVO
        }
        catch (AuthenticationException e)
        {
            statusText.text = "Error: " + e.Message;
            Debug.LogException(e); // >>> NUEVO
        }
        catch (RequestFailedException e)
        {
            statusText.text = "Error: " + e.Message;
            Debug.LogException(e); // >>> NUEVO
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
            Debug.Log("Login exitoso. Player ID: " + AuthenticationService.Instance.PlayerId); // >>> NUEVO
        }
        catch (AuthenticationException e)
        {
            statusText.text = "Error: " + e.Message;
            Debug.LogException(e); // >>> NUEVO
        }
        catch (RequestFailedException e)
        {
            statusText.text = "Error: " + e.Message;
            Debug.LogException(e); // >>> NUEVO
        }
    }

    public void OnLogoutButton()
    {
        AuthenticationService.Instance.SignOut();
        statusText.text = "Sesión cerrada.";
        usernameInput.text = "";
        passwordInput.text = "";
        Debug.Log("Sesión cerrada."); // >>> NUEVO
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

    public async void OnAnonymousLoginButton()
    {
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
            Debug.Log("Login anónimo exitoso. Player ID: " + AuthenticationService.Instance.PlayerId); // >>> NUEVO
        }
        catch (AuthenticationException e)
        {
            statusText.text = "Error: " + e.Message;
            Debug.LogException(e); // >>> NUEVO
        }
        catch (RequestFailedException e)
        {
            statusText.text = "Error: " + e.Message;
            Debug.LogException(e); // >>> NUEVO
        }
    }
}