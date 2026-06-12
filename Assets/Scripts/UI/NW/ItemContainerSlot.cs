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
        ItemSlot.PlaceInto(item.transform, transform);
    }

    public void ClearSlot()
    {
        // Destroy the UI element and clear the reference

        if (currentItem != null)
            Destroy(currentItem.gameObject);
        currentItem = null;
    }

    // Nullifica la referencia interna sin destruir el GameObject.
    // Llamado por ItemSlotDrag.OnEndDrag cuando el item se va de este slot
    // por drag exitoso a otro container. Distinto de ClearSlot(), que sí
    // destruye porque el llamador quiere eliminar el item de verdad.
    public void ClearReference()
    {
        currentItem = null;
    }

    // Returns the item assigned to this slot (can be null)
    public ItemSlot GetItem()
    {
        return currentItem;
    }
}