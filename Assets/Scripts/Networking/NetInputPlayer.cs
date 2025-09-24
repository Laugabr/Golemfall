using Fusion;
using UnityEngine;

public enum InputButton
{
    Jump
}

public struct NetInputPlayer : INetworkInput
{
    public NetworkButtons Buttons;
    public Vector2 Direction;

    
}
