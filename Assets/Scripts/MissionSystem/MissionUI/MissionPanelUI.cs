using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Fusion;

public class MissionPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private KeyCode toggleKey = KeyCode.M;

    [Header("Lista de misiones")]
    [SerializeField] private Transform missionListParent;
    [SerializeField] private GameObject missionItemPrefab;

    [Header("Refs")]
    [SerializeField] private MissionController missionController;

    [Header("Estilo (opcional)")]
    [SerializeField] private string objetivosHeader = "<color=#7CFF7C><b>OBJETIVOS</b></color>";
    [SerializeField] private string fallosHeader = "<color=#FF7C7C><b>NO DEBE OCURRIR</b></color>";
    [SerializeField] private string completedColor = "#7CFF7C";
    [SerializeField] private string pendingColor = "#FFFFFF";

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Start()
    {
        // DEBUG 1: ¿el script existe y arranca?
        Debug.Log($"[MissionPanelUI] Start → GO activo={gameObject.activeInHierarchy}, " +
                  $"panel asignado={(panel == null ? "NULL" : panel.name)}, " +
                  $"missionController asignado={(missionController == null ? "NULL" : missionController.name)}");
    }

    private void Update()
    {
        // Escape: solo cierra si está abierto (no abre).
        if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
        {
            Close();
            return;
        }

        if (!Input.GetKeyDown(toggleKey)) return;

        // DEBUG 2: ¿se detecta la tecla?
        Debug.Log($"[MissionPanelUI] Tecla {toggleKey} detectada");

        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || !runner.IsRunning)
        {
            Debug.Log("[MissionPanelUI] Runner null o no running, return");
            return;
        }

        TogglePanel();
    }

    private void OnEnable()
    {
        MissionEvents.OnMissionComplete    += OnMissionChanged;
        MissionEvents.OnMissionFailed      += OnMissionChanged;
        MissionEvents.OnMissionProgress    += OnMissionChanged;
        MissionEvents.OnMissionStarted     += OnMissionChanged;
        MissionEvents.OnMissionListChanged += RefreshMissions;
    }

    private void OnDisable()
    {
        MissionEvents.OnMissionComplete    -= OnMissionChanged;
        MissionEvents.OnMissionFailed      -= OnMissionChanged;
        MissionEvents.OnMissionProgress    -= OnMissionChanged;
        MissionEvents.OnMissionStarted     -= OnMissionChanged;
        MissionEvents.OnMissionListChanged -= RefreshMissions;
    }

    public void TogglePanel()
    {
        bool newState = !panel.activeSelf;
        panel.SetActive(newState);

        // DEBUG 3: ¿se ejecuta el toggle? ¿qué pasa con el panel?
        Debug.Log($"[MissionPanelUI] TogglePanel → panel.activeSelf ahora={panel.activeSelf}, " +
                  $"panel.activeInHierarchy={panel.activeInHierarchy}");

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (newState) RefreshMissions();
    }

    /// <summary>Cierra el panel de misiones. No hace nada si ya está cerrado.</summary>
    public void Close()
    {
        if (panel == null) return;

        panel.SetActive(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnMissionChanged(MissionData _)
    {
        if (panel.activeSelf) RefreshMissions();
    }

    private void RefreshMissions()
    {
        // DEBUG 4: ¿se llama refresh y cuántas misiones hay?
        Debug.Log($"[MissionPanelUI] RefreshMissions → " +
                  $"controller={(missionController == null ? "NULL" : missionController.name)}, " +
                  $"currentMissions={(missionController != null ? missionController.CurrentMissions.Count.ToString() : "N/A")}, " +
                  $"missionListParent={(missionListParent == null ? "NULL" : missionListParent.name)}, " +
                  $"missionItemPrefab={(missionItemPrefab == null ? "NULL" : missionItemPrefab.name)}");

        foreach (Transform child in missionListParent) Destroy(child.gameObject);

        if (missionController == null) return;

        foreach (var mission in missionController.CurrentMissions)
        {
            GameObject item = Instantiate(missionItemPrefab, missionListParent);
            TMP_Text text = item.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = BuildMissionText(mission);
        }
    }

    private string BuildMissionText(MissionData mission)
    {
        var sb = new StringBuilder();

        sb.Append("<b>").Append(mission.missionName).Append("</b>\n");

        if (!string.IsNullOrEmpty(mission.description))
            sb.Append("<size=80%>").Append(mission.description).Append("</size>\n");

        if (mission.missionSteps != null && mission.missionSteps.Count > 0)
        {
            sb.Append('\n').Append(objetivosHeader).Append('\n');
            foreach (var step in mission.missionSteps)
                AppendStepLine(sb, step);
        }

        if (mission.failureSteps != null && mission.failureSteps.Count > 0)
        {
            sb.Append('\n').Append(fallosHeader).Append('\n');
            foreach (var step in mission.failureSteps)
                AppendStepLine(sb, step);
        }

        return sb.ToString();
    }

    private void AppendStepLine(StringBuilder sb, MissionStep step)
    {
        string color = step.isComplete ? completedColor : pendingColor;
        string check = step.isComplete ? "✓" : "•";
        sb.Append("<color=").Append(color).Append(">")
          .Append(check).Append(' ')
          .Append(step.DisplayLabel)
          .Append(' ').Append(step.currentAmount).Append('/').Append(step.amount)
          .Append("</color>\n");
    }
}