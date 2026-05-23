using UnityEngine;
using System.Collections.Generic;

// Ficha de datos de un NPC: nombre visible y lista de diálogos posibles.
// Los diálogos se evalúan en orden — se usa el primero cuyo requirement.IsMet() devuelva true.
// Se crea como asset en el inspector: click derecho → Create → Dialogue → NpcData
[CreateAssetMenu(fileName = "NewNpc", menuName = "Dialogue/NpcData")]
public class NpcData : ScriptableObject
{
    public string npcId;
    public string displayName;
    public Sprite portrait;  // retrato por defecto, puede sobreescribirse por línea

    [Tooltip("Evaluados en orden. El primero que cumpla su condición se muestra.")]
    public List<DialogueData> dialogues = new();

    // Devuelve el primer diálogo cuya condición se cumple, o null si ninguno aplica.
    public DialogueData GetEligibleDialogue()
    {
        foreach (var dialogue in dialogues)
        {
            if (dialogue != null && dialogue.requirement.IsMet())
                return dialogue;
        }
        return null;
    }
}
