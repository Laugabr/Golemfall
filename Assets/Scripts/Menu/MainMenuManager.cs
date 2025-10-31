using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject creditsPanel;
    public GameObject settingsPanel;

    [Header("Optional Fade Settings")]
    public float fadeDuration = 0.5f;

    void Start()
    {
        ShowMainMenu();
    }

    // ------------------- BOTONES -------------------

    public void OnPlayButton()
    {
        // Cargar la escena del juego
        SceneManager.LoadScene("GameScene");
    }

    public void OnCreditsButton()
    {
        ShowPanel(creditsPanel);
    }

    public void OnSettingsButton()
    {
        ShowPanel(settingsPanel);
    }

    public void OnQuitButton()
    {
        // Salir del juego (funciona solo en build)
        Application.Quit();
        Debug.Log("Quit Game (solo funciona en build)");
    }

    // ------------------- RETORNO -------------------

    public void OnBackButton()
    {
        ShowMainMenu();
    }

    // ------------------- LÓGICA DE MOSTRAR -------------------

    void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        creditsPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    void ShowPanel(GameObject panel)
    {
        mainMenuPanel.SetActive(false);
        creditsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        panel.SetActive(true);
    }
}
