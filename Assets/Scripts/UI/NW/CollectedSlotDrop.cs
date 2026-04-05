using UnityEngine;
using UnityEngine.EventSystems;

public class CollectedSlotDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag?.GetComponent<ItemSlotDrag>();
        Debug.Log($"OnDrop — dragged:{dragged != null}");
        if (dragged == null) return;

        Debug.Log($"OnDrop — childCount:{transform.childCount}");
        if (transform.childCount > 0) return;

        bool cameFromEquip = dragged.originalParent.GetComponent<EquipSlotDrop>() != null;

        if (cameFromEquip)
        {
            var itemSlot = dragged.GetComponent<ItemSlot>();
            if (itemSlot == null) return;

            short itemKey = ItemData.GetKey(itemSlot.GetItemData());

            var ui = FindFirstObjectByType<NwInventoryUI>();
            var inventory = ui?.GetTargetInventory();
            if (inventory == null) return;

            inventory.RPC_RequestUnequip(itemKey);
        }

        dragged.transform.SetParent(transform);
        dragged.transform.localPosition = Vector3.zero;

        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        if (!cameFromEquip)
        {
            var ui = FindFirstObjectByType<NwInventoryUI>();
            ui?.SuppressNextRefresh();
        }
    }
}
