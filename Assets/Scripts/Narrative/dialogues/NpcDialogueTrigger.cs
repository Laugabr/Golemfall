using UnityEngine;

// Dispara y avanza el diálogo de este NPC al presionar F.
// La proximidad la maneja el ProximityInteractor del mismo GameObject (el mismo
// que muestra el prompt "F"): este trigger solo arranca el diálogo cuando ES el
// interactor enfocado, y solo avanza el diálogo que ARRANCÓ él (ownership), así
// dos NPCs que compartan el mismo NpcData no duplican el avance.
[RequireComponent(typeof(ProximityInteractor))]
public class NpcDialogueTrigger : MonoBehaviour
{
    [SerializeField] private NpcData npcData;

    private ProximityInteractor _interactor;
    private bool _isFocused;
    private bool _ownsCurrent;
    private bool _subscribedToController;

    private void Awake()
    {
        _interactor = GetComponent<ProximityInteractor>();
    }

    private void OnEnable()
    {
        ProximityInteractor.OnPlayerEntered += HandlePlayerEntered;
        ProximityInteractor.OnPlayerExited += HandlePlayerExited;
        TrySubscribeController();
    }

    private void Start()
    {
        // Por si el controller no estaba listo en OnEnable (orden de inicialización)
        TrySubscribeController();
    }

    private void OnDisable()
    {
        ProximityInteractor.OnPlayerEntered -= HandlePlayerEntered;
        ProximityInteractor.OnPlayerExited -= HandlePlayerExited;

        if (_subscribedToController && DialogueController.Instance != null)
        {
            DialogueController.Instance.OnDialogueEnded -= HandleDialogueEnded;
            _subscribedToController = false;
        }
    }

    private void TrySubscribeController()
    {
        if (_subscribedToController || DialogueController.Instance == null) return;
        DialogueController.Instance.OnDialogueEnded += HandleDialogueEnded;
        _subscribedToController = true;
    }

    private void HandlePlayerEntered(ProximityInteractor interactor)
    {
        if (interactor == _interactor) _isFocused = true;
    }

    private void HandlePlayerExited(ProximityInteractor interactor)
    {
        if (interactor == _interactor) _isFocused = false;
    }

    private void HandleDialogueEnded()
    {
        _ownsCurrent = false;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;

        var dialogue = DialogueController.Instance;
        if (dialogue == null) return;

        if (dialogue.IsInDialogue)
        {
            // Solo el NPC que arrancó este diálogo lo avanza. No depende del foco,
            // así el player puede terminar de leer aunque se haya alejado un poco.
            if (_ownsCurrent)
                dialogue.Advance();
        }
        else if (_isFocused)
        {
            // StartDialogue devuelve true solo si fue ESTE llamado el que arrancó.
            _ownsCurrent = dialogue.StartDialogue(npcData);
        }
    }
}