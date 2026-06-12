using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Manager client-only de notificaciones. No es NetworkBehaviour: cada cliente ve las suyas.
/// Cola FIFO; una notificación de prioridad mayor interrumpe la actual con fade.
/// </summary>
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("View")]
    [SerializeField] private NotificationView viewPrefab;
    [SerializeField] private Transform viewParent;

    [Header("Tuning")]
    [SerializeField, Min(0f)] private float fadeOutOnInterrupt = 0.15f;

    private readonly Queue<NotificationRequest> queue = new();
    private NotificationRequest current;
    private NotificationView activeView;
    private Coroutine showRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---- API pública ----

    /// <summary>API canónica: muestra una notificación a partir de un SO.</summary>
    public void Show(NotificationData data, params object[] formatArgs)
    {
        Debug.Log($"[NotifManager] Show: data={data?.name}, prefab={viewPrefab != null}, parent={viewParent?.name}");
        if (data == null)
        {
            Debug.LogWarning("[NotificationManager] Show llamado con data null.");
            return;
        }

        string finalText = (formatArgs != null && formatArgs.Length > 0)
            ? SafeFormat(data.text, formatArgs)
            : data.text;

        Enqueue(new NotificationRequest
        {
            text = finalText,
            icon = data.icon,
            category = data.category,
            priority = data.priority,
            duration = data.duration
        });
    }

    /// <summary>
    /// Overload que permite sobrescribir el ícono del SO en runtime.
    /// Útil cuando el ícono cambia por contexto (ej: notificación de craft
    /// que muestra el ícono del item resultante en lugar del ícono fijo del SO).
    /// </summary>
    public void Show(NotificationData data, Sprite iconOverride, params object[] formatArgs)
    {
        if (data == null)
        {
            Debug.LogWarning("[NotificationManager] Show llamado con data null.");
            return;
        }

        string finalText = (formatArgs != null && formatArgs.Length > 0)
            ? SafeFormat(data.text, formatArgs)
            : data.text;

        Enqueue(new NotificationRequest
        {
            text = finalText,
            icon = iconOverride != null ? iconOverride : data.icon,
            category = data.category,
            priority = data.priority,
            duration = data.duration
        });
    }

    /// <summary>Atajo de conveniencia para toasts ad-hoc desde código.</summary>
    public void ShowToast(string message, float duration = 2f)
    {
        Enqueue(new NotificationRequest
        {
            text = message,
            icon = null,
            category = NotificationCategory.Toast,
            priority = 0,
            duration = duration
        });
    }

    // ---- Internals ----

    private void Enqueue(NotificationRequest req)
    {
        // ¿Interrumpe la actual?
        if (current.IsValid && req.priority > current.priority)
        {
            if (showRoutine != null) StopCoroutine(showRoutine);
            showRoutine = StartCoroutine(InterruptAndShow(req));
            return;
        }

        // Si no hay nada activo, mostrar ya
        if (!current.IsValid)
        {
            current = req;
            showRoutine = StartCoroutine(ShowRoutine(req));
            return;
        }

        // Si hay algo activo de prioridad >=, va a la cola
        queue.Enqueue(req);
    }

    private IEnumerator InterruptAndShow(NotificationRequest next)
    {
        if (activeView != null)
            yield return activeView.FadeOut(fadeOutOnInterrupt);

        current = next;
        yield return ShowRoutineCore(next);
        AfterCurrentFinished();
    }

    private IEnumerator ShowRoutine(NotificationRequest req)
    {
        yield return ShowRoutineCore(req);
        AfterCurrentFinished();
    }

    private IEnumerator ShowRoutineCore(NotificationRequest req)
    {
        if (viewPrefab == null)
        {
            Debug.LogError("[NotificationManager] viewPrefab no asignado.");
            yield break;
        }

        activeView = Instantiate(viewPrefab, viewParent != null ? viewParent : transform);
        yield return activeView.Play(req);
        if (activeView != null) Destroy(activeView.gameObject);
        activeView = null;
    }

    private void AfterCurrentFinished()
    {
        current = default;
        showRoutine = null;

        if (queue.Count > 0)
        {
            var next = queue.Dequeue();
            current = next;
            showRoutine = StartCoroutine(ShowRoutine(next));
        }
    }

    private static string SafeFormat(string template, object[] args)
    {
        try { return string.Format(template, args); }
        catch (FormatException)
        {
            Debug.LogWarning($"[NotificationManager] Template inválido: \"{template}\". Usando texto crudo.");
            return template;
        }
    }
}

/// <summary>Datos runtime de una notificación encolada (independiente del SO de origen).</summary>
public struct NotificationRequest
{
    public string text;
    public Sprite icon;
    public NotificationCategory category;
    public int priority;
    public float duration;

    public bool IsValid => !string.IsNullOrEmpty(text) || icon != null;
}