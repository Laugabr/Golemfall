using UnityEngine;

public class EventDebugger : MonoBehaviour
{
    private void OnEnable()
    {
        TrackEvents.OnTrackEvent += DebugEvent;
    }

    private void OnDisable()
    {
        TrackEvents.OnTrackEvent -= DebugEvent;
    }

    void DebugEvent(GameEventType id, int amount)
    {
        Debug.Log($"EVENT RECEIVED → {id} | amount: {amount}");
    }
}
