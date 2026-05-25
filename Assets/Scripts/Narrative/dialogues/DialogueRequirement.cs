using UnityEngine;

public enum DialogueCondition
{
    None,            // siempre se muestra
    MissionNotStarted,
    MissionActive,
    MissionComplete
}

// Condición que determina si un DialogueData puede mostrarse.
// IsMet() consulta MissionController directamente — es una lectura puntual, sin estado propio.
[System.Serializable]
public class DialogueRequirement
{
    public DialogueCondition condition;
    public string missionId;

    public bool IsMet()
    {
        if (condition == DialogueCondition.None) return true;

        var controller = Object.FindFirstObjectByType<MissionController>();
        if (controller == null)
        {
            Debug.LogWarning("DialogueRequirement: MissionController no encontrado en la escena.");
            return false;
        }

        bool isActive   = false;
        bool isComplete = false;

        foreach (var m in controller.CurrentMissions)
        {
            if (m.missionId == missionId) { isActive = true; break; }
        }

        isComplete = controller.IsMissionComplete(missionId);

        return condition switch
        {
            DialogueCondition.MissionNotStarted => !isActive && !isComplete,
            DialogueCondition.MissionActive     => isActive,
            DialogueCondition.MissionComplete   => isComplete,
            _                                   => false
        };
    }
}
