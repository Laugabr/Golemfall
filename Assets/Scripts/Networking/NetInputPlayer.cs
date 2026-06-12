using Fusion;
using UnityEngine;

public enum InputButton
{
    Jump,           // 0
    Dash,           // 1
    Interact,       // 2
    BasicAttack,    // 3
    FirstSkill,     // 4
    SecondarySkill, // 5
    MouseButton0,   // 6 — click izquierdo acumulado (protección clicks rápidos en Fusion)
    MouseButton1,   // 7 — reservado para uso futuro
}

public struct NetInputPlayer : INetworkInput
{
    public NetworkButtons Buttons;

    public Vector2 Direction;

    public Vector2 LookDelta;

    /// <summary>
    /// Yaw (en grados) de la cámara del cliente que origina el input.
    /// El server lo usa para transformar Direction (espacio cámara) a espacio mundo,
    /// de modo que el movimiento sea idéntico para owner y proxies/server.
    /// </summary>
    public float CameraYaw;

    /// <summary>
    /// Yaw (en grados) hacia donde apunta el cursor del mouse relativo al jugador,
    /// calculado en el cliente cada frame como Atan2 de la dirección desde el jugador
    /// hasta el punto del mundo donde apunta el mouse.
    ///
    /// Se envía al servidor para que pueda escribir NetBodyYaw al simular el tick
    /// del cliente — el host no tiene acceso al mouse ni a la cámara del cliente,
    /// así que sin este campo la rotación de ataque solo funcionaría en el host local.
    ///
    /// Es el mismo tipo de ángulo que usa NetBodyYaw, por lo que puede asignarse
    /// directamente sin conversión adicional.
    /// </summary>
    public float AttackYaw;
}