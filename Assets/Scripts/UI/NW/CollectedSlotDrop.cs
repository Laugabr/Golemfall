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

        // AssignItem setea currentItem del container destino Y aplica el placement.
        // Sin esto, el slot tendría el item como child físico pero su currentItem seguiría null,
        // y el próximo Refresh lo vería como vacío e instanciaría un duplicado encima.
        var container = GetComponent<ItemContainerSlot>();
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