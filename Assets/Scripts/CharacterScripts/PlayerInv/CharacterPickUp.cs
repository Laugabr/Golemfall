using Fusion;
using UnityEngine;

public class CharacterPickUp : NetworkBehaviour
{
    private ProximityInteractor currentInteractor;

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

    public void TryPickUp()
    {
        if (currentInteractor == null) return;

        var item = currentInteractor.GetComponent<PickableItem>();
        if (item == null) return;

        Debug.Log($"{Object} tries to collect {item.Item.displayName}");
        item.Rpc_Collect(Object);
        TrackEvents.OnTrackEvent?.Invoke(GameEventType.CollectItem, 1, item.Item.missionKey);

        InteractPrompt.Instance?.Hide();
        MessageManager.Instance?.Show("Item recolectado");
        currentInteractor = null;
    }
}