using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text togglePasswordText;

    [Header("Buttons")]
    [SerializeField] private Button registerButton;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button anonymousLoginButton;

    [Header("Config")]
    [SerializeField] private string sceneToLoad = "Integration";

    async void Start()
    {
        SetButtonsInteractable(false);
        statusText.text = "Inicializando...";
        togglePasswordText.text = "(-)";

        try
        {
            await UnityServices.InitializeAsync();
            SetButtonsInteractable(true);
            statusText.text = "Listo para iniciar sesión.";
        }
        catch (System.Exception e)
        {
            statusText.text = "Error al inicializar servicios.";
            Debug.LogException(e);
        }
    }

    public async void OnRegisterButton()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            statusText.text = "¡Registro exitoso!";

            // Opcional: loguearlo automáticamente acá o esperar a que pulse Login?
            EnterGame();
        }
        catch (AuthenticationException e) { HandleError(e); }
        catch (RequestFailedException e) { HandleError(e); }
    }

    public async void OnLoginButton()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            EnterGame();
        }
        catch (AuthenticationException e) { HandleError(e); }
        catch (RequestFailedException e) { HandleError(e); }
    }

    public async void OnAnonymousLoginButton()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            EnterGame();
            return;
        }

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            EnterGame();
        }
        catch (AuthenticationException e) { HandleError(e); }
        catch (RequestFailedException e) { HandleError(e); }
    }

    private void EnterGame()
    {
        Debug.Log("Login exitoso. Player ID: " + AuthenticationService.Instance.PlayerId);
        // Cambia a la escena del juego
        SceneManager.LoadScene(sceneToLoad);
    }

    private void HandleError(System.Exception e)
    {
        statusText.text = "Error: " + e.Message;
        Debug.LogException(e);
    }

    private void SetButtonsInteractable(bool state)
    {
        registerButton.interactable = state;
        loginButton.interactable = state;
        anonymousLoginButton.interactable = state;
    }

    // --- Toggle Password Logic ---
    private bool passwordVisible = false;
    public void OnTogglePasswordButton()
    {
        passwordVisible = !passwordVisible;
        passwordInput.contentType = passwordVisible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        togglePasswordText.text = passwordVisible ? "(o)" : "(-)";
        passwordInput.ForceLabelUpdate();
    }
}