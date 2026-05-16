// NotificationView.cs — STUB para Fase 1, reemplazar en Fase 4
using System.Collections;
using UnityEngine;

public class NotificationView : MonoBehaviour
{
    public virtual IEnumerator Play(NotificationRequest req)
    {
        Debug.Log($"[Notif] [{req.category}] (p={req.priority}) {req.text}");
        yield return new WaitForSeconds(req.duration);
    }

    public virtual IEnumerator FadeOut(float duration)
    {
        yield return new WaitForSeconds(duration);
    }
}