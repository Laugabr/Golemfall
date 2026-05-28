using UnityEngine;
using System;

// Controla el flujo de un diálogo: arranca, avanza líneas, termina.
// Singleton client-only (no es NetworkBehaviour) — vive solo en el cliente.
// No dibuja UI ni lee input directo: expone eventos para que la UI se cuelgue
// y un método Advance() para que el sistema de input lo llame.
public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance { get; private set; }

    public enum DialogueState { Idle, Showing }
    public DialogueState State { get; private set; } = DialogueState.Idle;

    private NpcData _currentNpc;
    private DialogueData _currentDialogue;
    private int _currentLineIndex;

    public bool IsInDialogue => State != DialogueState.Idle;
    public NpcData CurrentNpc => _currentNpc;

    // Eventos para que la UI se suscriba (Fase 4)
    public event Action<NpcData, DialogueLine> OnDialogueStarted;
    public event Action<DialogueLine> OnLineChanged;
    public event Action OnDialogueEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Llamado desde Fase 5 cuando el player interactúa con un NPC.
    public void StartDialogue(NpcData npc)
    {
        if (IsInDialogue)
        {
            // Ya hay un diálogo activo — ignorar el segundo llamado
            return;
        }

        if (npc == null) return;

        var dialogue = npc.GetEligibleDialogue();
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Count == 0)
        {
            Debug.Log($"DialogueController: NPC {npc.npcId} no tiene diálogo elegible.");
            return;
        }

        _currentNpc = npc;
        _currentDialogue = dialogue;
        _currentLineIndex = -1;
        State = DialogueState.Showing;

        // Avanzar al primer índice válido
        if (!MoveToNextValidLine())
        {
            // Todas las líneas estaban vacías
            EndDialogue();
            return;
        }

        OnDialogueStarted?.Invoke(_currentNpc, _currentDialogue.lines[_currentLineIndex]);
        Debug.Log($"DialogueController: dialogue '{_currentDialogue.dialogueId}' started. Line: {_currentDialogue.lines[_currentLineIndex].text}");
    }

    // Llamado cuando el player aprieta el botón de avanzar.
    public void Advance()
    {
        if (!IsInDialogue) return;

        if (!MoveToNextValidLine())
        {
            EndDialogue();
            return;
        }

        OnLineChanged?.Invoke(_currentDialogue.lines[_currentLineIndex]);
        Debug.Log($"DialogueController: line {_currentLineIndex}: {_currentDialogue.lines[_currentLineIndex].text}");
    }

    // Avanza el índice hasta la próxima línea con texto. Devuelve false si no hay más.
    private bool MoveToNextValidLine()
    {
        for (int i = _currentLineIndex + 1; i < _currentDialogue.lines.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(_currentDialogue.lines[i].text))
            {
                _currentLineIndex = i;
                return true;
            }
        }
        return false;
    }

    private void EndDialogue()
    {
        var finished = _currentDialogue;

        // Limpiar estado primero — por si algún listener consulta IsInDialogue en OnDialogueEnded
        _currentNpc = null;
        _currentDialogue = null;
        _currentLineIndex = -1;
        State = DialogueState.Idle;

        // Disparar evento configurado en el diálogo (puede iniciar misiones, etc.)
        if (finished != null && finished.triggerEventOnComplete)
        {
            TrackEvents.OnTrackEvent?.Invoke(finished.onCompleteEvent, finished.onCompleteAmount);
            Debug.Log($"DialogueController: dialogue '{finished.dialogueId}' end → event {finished.onCompleteEvent} ({finished.onCompleteAmount})");
        }

        OnDialogueEnded?.Invoke();
        Debug.Log("DialogueController: dialogue ended.");
    }
}