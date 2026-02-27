using UnityEngine;
using System;

[System.Serializable]
public class MissionStep
{
    public string targetId; //Kill_Enemy, Pick_Item (general), Pick_Item_001 (específico), etc
    public int amount; //cuanta cantidad de enemigos por ej para completar la misión
    [NonSerialized]public bool isComplete; 
    [NonSerialized]public int currentAmount; //nonserialized es para que el game designer no lo toque y no lo vea en el inspector


    public void UpdateProgress(string id, int progress)
    {
        if(isComplete) return;

        if(string.CompareOrdinal(id, targetId) != 0) return;

        currentAmount += progress;

        if(currentAmount >= amount)
        {
            isComplete=true;
        }
    } 

}
