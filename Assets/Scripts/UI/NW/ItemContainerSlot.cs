using UnityEngine;

/*
  ItemContainerSlot
 
  Represents a UI slot that can hold a single ItemSlot.
  Handles assigning, clearing, and retrieving items, while ensuring
  proper UI layout adjustments (position and scaling).
 */

public class ItemContainerSlot : MonoBehaviour
{
    [SerializeField] private ItemSlot currentItem; // Currently assigned UI item


    // Returns true if the slot has no assigned item
    public bool IsEmpty => currentItem == null;

    public void AssignItem(ItemSlot item)
    {
        currentItem = item;

        // Attach the item to this UI slot

        var rt = item.transform as RectTransform;

        // Adjust RectTransform properties for correct UI placement

        item.transform.SetParent(transform, false);
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    public void ClearSlot()
    {
        // Destroy the UI element and clear the reference

        if (currentItem != null)
            Destroy(currentItem.gameObject);
        currentItem = null;
    }

    // Returns the item assigned to this slot (can be null)
    public ItemSlot GetItem()
    {
        return currentItem;
    }
}

