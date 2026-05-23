using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "NotificationData", menuName = "Game/Notifications/Notification Data")]
public class NotificationData : ScriptableObject
{
    [Tooltip("Identificador opcional para debug/log. No se muestra al jugador.")]
    public string id;

    [Tooltip("Texto que ve el jugador. Soporta {0} para interpolar (ej: nombre de misión).")]
    [TextArea(2, 4)]
    public string text;

    [Tooltip("Ícono opcional. Si es null, la View oculta el slot del ícono.")]
    public Sprite icon;

    [Tooltip("Define el layout y la posición en pantalla.")]
    public NotificationCategory category = NotificationCategory.Toast;

    [Tooltip("Mayor número = mayor prioridad. Una notificación con prioridad mayor interrumpe la actual.\n0 = Toast, 10 = Mission, 20 = LevelUp (sugerido).")]
    public int priority = 0;

    [Tooltip("Duración total en pantalla, en segundos (incluye fades).")]
    [Min(0.1f)]
    public float duration = 2.5f;
}
