using Fusion;
using UnityEngine;

public enum InputButton
{
    Jump,
    Dash,
    Interact,
    BasicAttack,    // Mouse izquierdo — ataque melee
    FirstSkill,     // Q — ataque a distancia
    SecondarySkill  // E — habilidad secundaria
}

public struct NetInputPlayer : INetworkInput
{
    public NetworkButtons Buttons;

    public Vector2 Direction;

    public Vector2 LookDelta;

    public const byte MOUSE_BUTTON_0 = 1;

    // MOUSE_BUTTON_1 ya no se usa para skills — el botón derecho es control de cámara
    public const byte MOUSE_BUTTON_1 = 2;
}