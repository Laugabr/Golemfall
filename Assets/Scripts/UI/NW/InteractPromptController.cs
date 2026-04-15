using UnityEngine;

public class InteractPromptController : MonoBehaviour
{
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
        InteractPrompt.Instance?.Show(interactor.transform, interactor.PromptText);
    }

    private void HandleExited(ProximityInteractor interactor)
    {
        InteractPrompt.Instance?.Hide();
    }
}
