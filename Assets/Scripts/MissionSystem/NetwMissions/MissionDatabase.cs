using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Missions/Mission Database")]
public class MissionDatabase : ScriptableObject
{
    [SerializeField] private List<MissionData> missions;

    private Dictionary<short, MissionData> idToMission;
    private Dictionary<MissionData, short> missionToId;

    private void Init()
    {
        if (idToMission != null) return;

        idToMission = new Dictionary<short, MissionData>();
        missionToId = new Dictionary<MissionData, short>();

        short i = 1;
        foreach (var m in missions)
        {
            idToMission[i] = m;
            missionToId[m] = i;
            i++;
        }
    }

    public short GetId(MissionData mission)
    {
        Init();
        return missionToId[mission];
    }

    public MissionData GetMission(short id)
    {
        Init();
        return idToMission.TryGetValue(id, out var m) ? m : null;
    }
}
