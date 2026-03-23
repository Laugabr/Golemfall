using System;

public static class MissionEvents
{
    public static Action<MissionData> OnMissionComplete;
    public static Action<MissionData> OnMissionFailed;
    public static Action<MissionData> OnMissionProgress;
    public static Action<MissionData> OnMissionStarted;
    public static Action OnMissionListChanged;
}

