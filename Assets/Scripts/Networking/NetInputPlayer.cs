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
}