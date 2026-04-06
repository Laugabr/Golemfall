using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LogOutManager : MonoBehaviour
{

    [Header("Buttons")]
    [SerializeField] private Button logoutButton;


    [Header("Config")]
    [SerializeField] private string sceneToSignIn = "AuthenticationScene";

    public void OnLogOut()
    {

        AuthenticationService.Instance.SignOut();
        Debug.Log("Log Out Player");

        SceneManager.LoadScene(sceneToSignIn);
    }
}