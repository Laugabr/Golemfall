using UnityEngine;
using UnityEngine.EventSystems;

public class EquipSlotDrop : MonoBehaviour, IDropHandler, IItemDropTarget
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag?.GetComponent<ItemSlotDrag>();
        if (dragged == null) return;
        TryAccept(dragged);
    }

    // Lógica de aterrizaje compartida por el drag (OnDrop) y el doble-click (ItemQuickMove).
    public bool TryAccept(ItemSlotDrag dragged)
    {
        if (dragged == null) return false;

        // Ocupación por estado tipado (currentItem), no por childCount físico:
        // así un child decorativo (background/highlight/VFX) no hace ver el slot
        // como ocupado. Fallback a childCount solo si no hay ItemContainerSlot.
        var container = GetComponent<ItemContainerSlot>();
        bool occupied = container != null ? !container.IsEmpty : transform.childCount > 0;
        if (occupied) return false;

        var itemSlot = dragged.GetComponent<ItemSlot>();
        if (itemSlot == null) return false;

        short itemKey = ItemData.GetKey(itemSlot.GetItemData());

        var ui = FindFirstObjectByType<NwInventoryUI>();
        var inventory = ui?.GetTargetInventory();
        if (inventory == null) return false;

        inventory.RPC_RequestEquip(itemKey);

        if (container != null)
            container.AssignItem(itemSlot);
        else
            ItemSlot.PlaceInto(dragged.transform, transform);

        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        return true;
    }
}