using UnityEngine;

public class ExamplePicker : MonoBehaviour
{
   public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<ExampleItem>(out var item))
        {
            TrackEvents.OnTrackEvent?.Invoke(MissionStepType.CollectItem, 1);
        }
    }
}
