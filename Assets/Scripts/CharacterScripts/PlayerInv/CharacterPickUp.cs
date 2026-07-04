using Fusion;
using UnityEngine;

public class CharacterPickUp : NetworkBehaviour
{
    [SerializeField] private NetCharacterAnimator characterAnimator; // ← NUEVO

    private ProximityInteractor currentInteractor;

    [Networked] private NetworkButtons PreviousButtons { get; set; } // ← NUEVO

    private void Awake() // ← NUEVO
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<NetCharacterAnimator>();
    }

    private void OnEnable()
    {
        ProximityInteractor.OnPlayerEntered += HandleEntered;
        ProximityInteractor.OnPlayerExited += HandleExited;
    }

    private void OnDisable()
    {
        ProximityInteractor.OnPlayerEntered -= HandleEntered;
        ProximityInteractor.OnPlayerExited -= HandleExited;
    }

    private void HandleEntered(ProximityInteractor interactor)
    {
        currentInteractor = interactor;
    }

    private void HandleExited(ProximityInteractor interactor)
    {
        if (currentInteractor == interactor)
            currentInteractor = null;
    }

    // ← NUEVO: lee el input de F (Interact) acá mismo. Solo llama a TryPickUp()
    // si hay un item cerca (currentInteractor != null); si no hay nada, F no hace nada
    // y no interrumpe ninguna otra animación.
    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetInputPlayer input)) return;

        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Interact) && currentInteractor != null)
        {
            TryPickUp();
        }

        PreviousButtons = input.Buttons;
    }

    public void TryPickUp()
    {
        if (currentInteractor == null) return;

        var item = currentInteractor.GetComponent<PickableItem>();
        if (item == null) return;

        //Debug.Log($"{Object} tries to collect {item.Item.displayName}");
        item.Rpc_Collect(Object);
        TrackEvents.OnTrackEvent?.Invoke(GameEventType.CollectItem, 1, item.Item.missionKey);

        if (characterAnimator != null) // ← NUEVO
            characterAnimator.TriggerPickupAnimation();

        InteractPrompt.Instance?.Hide();
        MessageManager.Instance?.Show("Item recolectado");
        currentInteractor = null;
    }
}