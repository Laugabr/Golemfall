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

    async void Start()
    {
        await UnityServices.InitializeAsync();
        statusText.text = "Listo para iniciar sesión.";
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
    }
}