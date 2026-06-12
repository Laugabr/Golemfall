using UnityEngine;
using UnityEngine.EventSystems;

public class EquipSlotDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag?.GetComponent<ItemSlotDrag>();
        if (dragged == null) return;
        if (transform.childCount > 0) return;

        var itemSlot = dragged.GetComponent<ItemSlot>();
        if (itemSlot == null) return;

        short itemKey = ItemData.GetKey(itemSlot.GetItemData());

        var ui = FindFirstObjectByType<NwInventoryUI>();
        var inventory = ui?.GetTargetInventory();
        if (inventory == null) return;

        inventory.RPC_RequestEquip(itemKey);

        ItemSlot.PlaceInto(dragged.transform, transform);

        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }
}