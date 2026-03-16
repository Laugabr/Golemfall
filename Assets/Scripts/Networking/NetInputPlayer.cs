using Fusion;
using UnityEngine;

public enum InputButton
{
    Jump,
    Dash,
    Interact,
    
    BasicAttack,
    FirstSkill,
    SecondarySkill
    
}

public struct NetInputPlayer : INetworkInput
{
    public NetworkButtons Buttons;

    public Vector2 Direction;

    public Vector2 LookDelta; 

    public const byte MOUSE_BUTTON_0 = 1;

    public const byte MOUSE_BUTTON_1 = 2;
    
}
