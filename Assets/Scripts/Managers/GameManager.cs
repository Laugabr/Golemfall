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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameState.Playing)
                PauseGame();
            else if (CurrentState == GameState.Paused)
                ResumeGame();
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        CurrentState = GameState.Paused;

        // Avisamos al PanelsManager
        if (PanelsManager.Instance != null)
            PanelsManager.Instance.ShowPauseMenu(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.Playing;

        if (PanelsManager.Instance != null)
            PanelsManager.Instance.ShowPauseMenu(false);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.MainMenu;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}


