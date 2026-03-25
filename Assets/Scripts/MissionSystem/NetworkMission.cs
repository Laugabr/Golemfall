using Fusion;
using UnityEngine;

public struct NetworkMission : INetworkStruct
{
    public short missionId;

    public byte stepIndex;
    public short currentAmount;

    public NetworkBool isComplete;
    public NetworkBool isFailed;
}
