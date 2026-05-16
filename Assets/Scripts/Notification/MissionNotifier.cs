using UnityEngine;

/// <summary>
/// Adapter: traduce MissionEvents a notificaciones.
/// Poner este componente en la misma escena que el NotificationManager (puede ser el mismo GameObject).
/// </summary>
public class MissionNotifier : MonoBehaviour
{
    [Header("Templates (usar {0} para el nombre de la misión)")]
    [SerializeField] private NotificationData missionStartedTemplate;
    [SerializeField] private NotificationData missionCompleteTemplate;
    [SerializeField] private NotificationData missionFailedTemplate;

    private void OnEnable()
    {
        MissionEvents.OnMissionStarted += HandleStarted;
        MissionEvents.OnMissionComplete += HandleComplete;
        MissionEvents.OnMissionFailed += HandleFailed;
    }

    private void OnDisable()
    {
        MissionEvents.OnMissionStarted -= HandleStarted;
        MissionEvents.OnMissionComplete -= HandleComplete;
        MissionEvents.OnMissionFailed -= HandleFailed;
    }

    private void HandleStarted(MissionData m) => Notify(missionStartedTemplate, m);
    private void HandleComplete(MissionData m) => Notify(missionCompleteTemplate, m);
    private void HandleFailed(MissionData m) => Notify(missionFailedTemplate, m);

    private void Notify(NotificationData template, MissionData mission)
    {
        if (template == null || mission == null) return;
        if (NotificationManager.Instance == null) return;
        NotificationManager.Instance.Show(template, mission.missionName);
    }
}

