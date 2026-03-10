using UnityEngine;
using System;
using TMPro;

public class MissionPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [SerializeField] private Transform missionListParent;
    [SerializeField] private GameObject missionItemPrefab;

    [SerializeField] private MissionController missionController;

    void OnEnable()
    {
        MissionEvents.OnMissionComplete += OnMissionChanged;
        MissionEvents.OnMissionFailed += OnMissionChanged;
        MissionEvents.OnMissionProgress += OnMissionChanged;
        MissionEvents.OnMissionStarted += OnMissionChanged;
        MissionEvents.OnMissionListChanged += RefreshMissions;
    }

    void OnDisable()
    {
        MissionEvents.OnMissionComplete -= OnMissionChanged;
        MissionEvents.OnMissionFailed -= OnMissionChanged;
        MissionEvents.OnMissionProgress -= OnMissionChanged;
        MissionEvents.OnMissionStarted -= OnMissionChanged;
        MissionEvents.OnMissionListChanged -= RefreshMissions;
    }

    public void TogglePanel()
    {
        bool newState = !panel.activeSelf;
        panel.SetActive(newState);

        if (newState)
        {
            RefreshMissions();
        }
    }

    void RefreshMissions()
    {
        foreach (Transform child in missionListParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var mission in missionController.CurrentMissions)
        {
            GameObject item = Instantiate(missionItemPrefab, missionListParent);

            TMP_Text text = item.GetComponentInChildren<TMP_Text>();
            string missionText = mission.missionId + "\n";

            foreach (var step in mission.missionSteps)
            {
                missionText += $"- {step.targetId} {step.currentAmount}/{step.amount}\n";
            }

            text.text = missionText;
        }
    }

    void OnMissionChanged(MissionData mission)
    {
        if (panel.activeSelf)
            RefreshMissions();
    }
}