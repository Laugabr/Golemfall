using UnityEngine;
using UnityEngine.EventSystems;

public class CollectedSlotDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag?.GetComponent<ItemSlotDrag>();
        Debug.Log($"OnDrop — dragged:{dragged != null}");
        if (dragged == null) return;

        // Ocupación por estado tipado, no por childCount físico (ver EquipSlotDrop).
        var container = GetComponent<ItemContainerSlot>();
        bool occupied = container != null ? !container.IsEmpty : transform.childCount > 0;
        if (occupied) return;

        var itemSlot = dragged.GetComponent<ItemSlot>();
        if (itemSlot == null) return;

        bool cameFromEquip = dragged.originalParent.GetComponent<EquipSlotDrop>() != null;

        if (cameFromEquip)
        {
            short itemKey = ItemData.GetKey(itemSlot.GetItemData());

            var ui = FindFirstObjectByType<NwInventoryUI>();
            var inventory = ui?.GetTargetInventory();
            if (inventory == null) return;

            inventory.RPC_RequestUnequip(itemKey);
        }

        if (container != null)
            container.AssignItem(itemSlot);
        else
            ItemSlot.PlaceInto(dragged.transform, transform);

        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        if (!cameFromEquip)
        {
            var ui = FindFirstObjectByType<NwInventoryUI>();
            ui?.SuppressNextRefresh();
        }
    }
}