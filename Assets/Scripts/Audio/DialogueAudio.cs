using UnityEngine;

/// <summary>
/// Reproduce un SFX 2D en cada línea de diálogo. Se cuelga de los eventos del
/// DialogueController (no toca su lógica). Poné este componente en el mismo objeto
/// que el DialogueBoxView (o cualquier objeto de la escena de diálogo).
/// </summary>
public class DialogueAudio : MonoBehaviour
{
    [SerializeField] private SoundEvent lineSound;

    private void OnEnable()
    {
        if (DialogueController.Instance == null) return;
        DialogueController.Instance.OnDialogueStarted += HandleStarted;
        DialogueController.Instance.OnLineChanged += HandleLineChanged;
    }

    private void OnDisable()
    {
        if (DialogueController.Instance == null) return;
        DialogueController.Instance.OnDialogueStarted -= HandleStarted;
        DialogueController.Instance.OnLineChanged -= HandleLineChanged;
    }

    private void HandleStarted(NpcData npc, DialogueLine line) => lineSound?.Play2D();
    private void HandleLineChanged(DialogueLine line) => lineSound?.Play2D();
}
