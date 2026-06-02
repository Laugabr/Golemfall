using System.Collections;
using UnityEngine;

/// <summary>
/// Adapter: traduce BasicEventsManager.OnLevelUp a una notificación.
/// Colapsa varios level-ups que ocurren en cascada (ej: al cargar XP de un save
/// donde CheckLevelUp recursa 1→2→3→4) en una sola notificación con el nivel más alto.
/// </summary>
public class LevelUpNotifier : MonoBehaviour
{
    [Tooltip("Template de notificación para subir de nivel. Usá {0} para el nuevo nivel.")]
    [SerializeField] private NotificationData levelUpTemplate;

    [Tooltip("Ventana en segundos durante la cual múltiples level-ups se colapsan en uno solo (se muestra el nivel más alto).")]
    [SerializeField, Min(0f)] private float coalesceWindow = 0.15f;

    private int pendingLevel = -1;
    private Coroutine flushRoutine;

    private void OnEnable() => BasicEventsManager.OnLevelUp += HandleLevelUp;

    private void OnDisable()
    {
        BasicEventsManager.OnLevelUp -= HandleLevelUp;
        if (flushRoutine != null) StopCoroutine(flushRoutine);
        flushRoutine = null;
        pendingLevel = -1;
    }

    private void HandleLevelUp(int newLevel)
    {
        // Nos quedamos con el nivel más alto recibido dentro de la ventana.
        if (newLevel > pendingLevel) pendingLevel = newLevel;

        if (flushRoutine == null)
            flushRoutine = StartCoroutine(FlushAfterDelay());
    }

    private IEnumerator FlushAfterDelay()
    {
        yield return new WaitForSeconds(coalesceWindow);

        int levelToShow = pendingLevel;
        pendingLevel = -1;
        flushRoutine = null;

        if (levelToShow < 0) yield break;
        if (levelUpTemplate == null) yield break;
        if (NotificationManager.Instance == null) yield break;

        NotificationManager.Instance.Show(levelUpTemplate, levelToShow);
    }
}