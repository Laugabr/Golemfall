using System;
using UnityEngine;

public static class BasicEventsManager
{
    public static Action<int> OnExperienceGain;
    public static Action<int> OnLevelUp;
    public static Action<int> OnInventoryCountChanged;

}
