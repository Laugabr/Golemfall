using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Paused,
    MainMenu
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private string mainMenuSceneName = "Menu";

    public GameState CurrentState { get; private set; } = GameState.Playing;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // GameManager es DontDestroyOnLoad y sobrevive el cambio de escena, pero
        // PanelsManager no: en la escena de gameplay no existe. Sin UI de pausa,
        // pausar solo dejaría un freeze fantasma (timeScale 0 sin panel). Por eso,
        // si no hay PanelsManager en escena, ignoramos Escape acá. En gameplay el
        // cierre de paneles lo manejan los propios paneles (InventoryToggle / MissionPanelUI).
        if (PanelsManager.Instance == null) return;

        if (CurrentState == GameState.Playing)
            PauseGame();
        else if (CurrentState == GameState.Paused)
            ResumeGame();
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        CurrentState = GameState.Paused;

        if (PanelsManager.Instance != null)
            PanelsManager.Instance.ShowPanel(PanelType.Pause);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.Playing;

        if (PanelsManager.Instance != null)
            PanelsManager.Instance.OnCloseAllButton();
    }

    public void GoToMainMenu()
{
    Time.timeScale = 1f;
    CurrentState = GameState.MainMenu;

    if (PanelsManager.Instance != null)
        PanelsManager.Instance.OnCloseAllButton();

    SceneManager.LoadScene(mainMenuSceneName);
}
}