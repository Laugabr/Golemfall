
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using System.Net;


public class MissionController : NetworkBehaviour
{
    private MissionData _currentMission; // esto es para una sola mision. si está permitido tener mas de una mision al mismo tiempo entonces esto pasa a ser una lista
    public MissionData CurrentMission => _currentMission;

    public void StartNewMission(MissionData missionData)
    {
        if (_currentMission != null)
        {
            Destroy(_currentMission);
        }
        _currentMission = Instantiate(missionData);
    }

    //Kill_Enemy, 1 ejemplo de parametros
    public void TrackStep(string stepId, int progress)
    {
        //Verificar que haya una misión en curso
        if (_currentMission == null) return;

        // si no hay condicion de completo o fallo la mision simplemente le hago el return
        if (!_currentMission.UpdateProgress(stepId, progress, out var isSuccess)) return;

        if (isSuccess)
        {
            //CompleteMission();
        }
        else
        {
            //FailureMission();
        }
    }

    private void CompleteMission()
    {
        Destroy(_currentMission);
        _currentMission = null;
        //llamar a evento de ui
        //guardar el estado de la mision
    }

    private void FailureMission()
    {
        Destroy(_currentMission);
        _currentMission = null;
        //llamar a evento de ui
    }

    private void OnDestroy()
    {
        if (_currentMission != null)
        {
            Destroy(_currentMission);
        }
    }


}
