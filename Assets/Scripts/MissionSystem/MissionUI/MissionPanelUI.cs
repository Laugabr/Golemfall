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

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey)) return;

        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || !runner.IsRunning) return;

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

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (newState) RefreshMissions();
    }

    private void OnMissionChanged(MissionData _)
    {
        if (panel.activeSelf) RefreshMissions();
    }

    private void RefreshMissions()
    {
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

        // Objetivos
        if (mission.missionSteps != null && mission.missionSteps.Count > 0)
        {
            sb.Append('\n').Append(objetivosHeader).Append('\n');
            foreach (var step in mission.missionSteps)
                AppendStepLine(sb, step);
        }

        // Fallos
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