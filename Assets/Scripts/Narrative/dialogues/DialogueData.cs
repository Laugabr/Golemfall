using UnityEngine;
using System.Collections.Generic;

// Un diálogo completo: lista de líneas, condición para mostrarse,
// y evento opcional que se dispara al terminar.
// Se crea como asset en el inspector: click derecho → Create → Dialogue → DialogueData
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/DialogueData")]
public class DialogueData : ScriptableObject
{
    public string dialogueId;

    [Tooltip("Condición que debe cumplirse para que este diálogo sea elegible.")]
    public DialogueRequirement requirement;

    public List<DialogueLine> lines = new();

    [Header("Evento al completar (opcional)")]
    [Tooltip("Evento que se dispara vía TrackEvents cuando termina el diálogo. None = sin efecto.")]
    public GameEventType onCompleteEvent;
    public int onCompleteAmount = 1;

    [Tooltip("Si está en false, no se dispara ningún evento al terminar.")]
    public bool triggerEventOnComplete = false;
}
