using UnityEngine;
using UnityEngine.SceneManagement;

public enum PanelType
{
    None,
    Pause,
    Inventory,
    Map
}

public class PanelsManager : MonoBehaviour
{
    public static PanelsManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject mapPanel;

    private PanelType currentPanel = PanelType.None;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        HideAllPanels();

         // Reset automático al cargar escenas
        SceneManager.sceneLoaded += (scene, mode) => HideAllPanels();
    }

    // Oculta todos los paneles
    private void HideAllPanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);

        currentPanel = PanelType.None;
    }

    // Muestra un panel y oculta los demás
    public void ShowPanel(PanelType panelType)
    {
        HideAllPanels();

        switch (panelType)
        {
            case PanelType.Pause:
                if (pausePanel != null) pausePanel.SetActive(true);
                break;
            case PanelType.Inventory:
                if (inventoryPanel != null) inventoryPanel.SetActive(true);
                break;
            case PanelType.Map:
                if (mapPanel != null) mapPanel.SetActive(true);
                break;
            case PanelType.None:
                // No mostrar nada
                break;
        }

        currentPanel = panelType;
    }

    // Alternar panel (abrir si está cerrado, cerrar si está abierto)
    public void TogglePanel(PanelType panelType)
    {
        if (currentPanel == panelType)
        {
            HideAllPanels();
        }
        else
        {
            ShowPanel(panelType);
        }
    }

    // --- BOTONES ---
    public void OnContinueButton()
    {
        GameManager.Instance.ResumeGame();
    }

    public void OnMainMenuButton()
    {
        GameManager.Instance.GoToMainMenu();
    }

    public void OnPauseButton()
    {
        ShowPanel(PanelType.Pause);
    }

    public void OnInventoryButton()
    {
        ShowPanel(PanelType.Inventory);
    }

    public void OnMapButton()
    {
        ShowPanel(PanelType.Map);
    }

    public void OnCloseAllButton()
    {
        HideAllPanels();
    }
}


