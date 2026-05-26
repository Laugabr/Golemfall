using UnityEngine;

/// <summary>
/// Adapter: traduce BasicEventsManager.OnLevelUp a una notificación.
/// </summary>
public class LevelUpNotifier : MonoBehaviour
{
    [Tooltip("Template de notificación para subir de nivel. Usá {0} para el nuevo nivel.")]
    [SerializeField] private NotificationData levelUpTemplate;

    private void OnEnable() => BasicEventsManager.OnLevelUp += HandleLevelUp;
    private void OnDisable() => BasicEventsManager.OnLevelUp -= HandleLevelUp;

    private void HandleLevelUp(int newLevel)
    {
        if (levelUpTemplate == null) return;
        if (NotificationManager.Instance == null) return;
        NotificationManager.Instance.Show(levelUpTemplate, newLevel);
    }
}

