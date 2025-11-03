using UnityEngine;

public class PanelsManager : MonoBehaviour
{
    public static PanelsManager Instance { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject mapPanel;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        pausePanel.SetActive(false);
    }

    public void ShowPauseMenu(bool show)
    {
        if (pausePanel != null)
            pausePanel.SetActive(show);
    }

    public void OnContinueButton()
    {
        GameManager.Instance.ResumeGame();
    }

    public void OnMainMenuButton()
    {
        GameManager.Instance.GoToMainMenu();
    }
    public void ShowInventory(bool show)
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(show);
    }
     public void ShowMap(bool show)
    {
        if (mapPanel != null)
            mapPanel.SetActive(show);
    }
}

