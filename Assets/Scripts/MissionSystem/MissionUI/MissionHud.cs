using UnityEngine;
using System;
using System.Collections.Generic;

public class MissionHud : MonoBehaviour
{
    private void OnEnable()
    {
        MissionEvents.OnMissionComplete += OnMissionComplete;
        MissionEvents.OnMissionFailed += OnMissionFailed;
    }

    private void OnDisable()
    {
        MissionEvents.OnMissionComplete -= OnMissionComplete;
        MissionEvents.OnMissionFailed -= OnMissionFailed;
    }

    private void OnMissionComplete(MissionData data)
    {
        // mostrar popup de victoria o datos de la mision
    }

    private void OnMissionFailed(MissionData data)
    {
        // mostrar popup
    }
}
