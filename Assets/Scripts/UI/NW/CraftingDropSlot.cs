using UnityEngine;
using UnityEngine.EventSystems;

public class CraftingDropSlot : MonoBehaviour, IDropHandler, IItemDropTarget
{
    public CraftingUI craftingUI;

    // Estado tipado del item actual. Evita que la lógica externa
    // tenga que mirar transform.childCount / GetChild(0) para saber
    // qué hay en el slot, lo que rompía el flujo si el prefab del slot
    // tenía cualquier child decorativo (background, highlight, VFX, etc).
    public ItemSlot CurrentItem { get; private set; }
    public bool IsEmpty => CurrentItem == null;

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
        if (!IsEmpty) return false;

        var itemSlot = dragged.GetComponent<ItemSlot>();
        if (itemSlot == null) return false;

        // Si el item venía de un slot de equip, desequiparlo en red al pasar a craft.
        // Antes esto no ocurría: el item quedaba "equipado" en red pero visualmente
        // en el slot de craft (estado inconsistente). Aplica a drag y a doble-click.
        bool cameFromEquip = dragged.originalParent != null
            && dragged.originalParent.GetComponent<EquipSlotDrop>() != null;
        if (cameFromEquip)
        {
            short itemKey = ItemData.GetKey(itemSlot.GetItemData());
            var ui = FindFirstObjectByType<NwInventoryUI>();
            var inventory = ui?.GetTargetInventory();
            if (inventory == null) return false;

            inventory.RPC_RequestUnequip(itemKey);
        }

        ItemSlot.PlaceInto(dragged.transform, transform);
        CurrentItem = itemSlot;

        craftingUI.UpdatePreview();
        return true;
    }

    public void Clear()
    {
        if (CurrentItem != null)
            Destroy(CurrentItem.gameObject);
        CurrentItem = null;
    }

    // Nullifica la referencia interna sin destruir el GameObject.
    // Llamado por ItemSlotDrag.OnEndDrag / ItemQuickMove cuando el item se va de
    // este slot. Distinto de Clear(), que sí destruye porque el llamador quiere
    // eliminar el item (ej: post-craft).
    public void ClearReference()
    {
        CurrentItem = null;
        craftingUI?.UpdatePreview();
    }

    // Key del item que ocupa este slot, o -1 si está vacío.
    // Lo usa NwInventoryUI.Refresh para excluir del inventario las keys que
    // están en un slot de craft (igual que excluye las equipadas), evitando
    // que un refresh de red duplique visualmente el item.
    public short GetOccupiedKey()
    {
        var data = CurrentItem != null ? CurrentItem.GetItemData() : null;
        return data != null ? ItemData.GetKey(data) : (short)-1;
    }
}