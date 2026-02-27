using UnityEngine;

public class ExamplePicker : MonoBehaviour
{
   public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<ExampleItem>(out var item))
        {
            TrackEvents.OnTrackEvent?.Invoke($"Pick_Item_{item.itemId}_", item.amount);
        }
    }
}
