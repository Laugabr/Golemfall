using UnityEngine;
using UnityEngine.EventSystems;

// Zona de descarte: todo el área de pantalla que queda POR FUERA del InventoryPanel.
// Soltar un item acá lo saca del inventario (o del equipo) y lo devuelve al mundo,
// sobre la cabeza del player. Ver NetworkInventory.RPC_RequestDiscard.
//
// Es el cuarto IItemDropTarget del sistema, junto a EquipSlotDrop, CollectedSlotDrop
// y CraftingDropSlot: reutiliza el mismo contrato de aterrizaje, así el descarte no
// abre un camino paralelo que haya que mantener aparte del drag and drop existente.
//
// Setup en escena:
//   - Image transparente (alpha 0) con Raycast Target ON, estirada a pantalla completa.
//   - Hermana del InventoryPanel y PRIMER sibling (detrás de todo).
//   - Activa/inactiva junto con el panel.
// Como solo recibe los raycasts que ni el panel ni ningún slot capturaron antes,
// "soltar acá" equivale exactamente a "soltar afuera del panel".
//
// A propósito NO se agrega al anillo de ItemQuickMove: un doble-click nunca debe
// tirar un item por accidente. Descartar exige el gesto explícito de arrastrar afuera.
public class InventoryDiscardZone : MonoBehaviour, IDropHandler, IItemDropTarget
{
    private NwInventoryUI inventoryUI;

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag?.GetComponent<ItemSlotDrag>();
        if (dragged == null) return;
        TryAccept(dragged);
    }

    public bool TryAccept(ItemSlotDrag dragged)
    {
        if (dragged == null) return false;

        var itemSlot = dragged.GetComponent<ItemSlot>();
        if (itemSlot == null) return false;

        short itemKey = ItemData.GetKey(itemSlot.GetItemData());
        if (itemKey < 0) return false;

        if (inventoryUI == null) inventoryUI = FindFirstObjectByType<NwInventoryUI>();
        var inventory = inventoryUI?.GetTargetInventory();
        if (inventory == null) return false;

        // El cliente solo manda la key. El server valida que el item exista y que
        // tenga worldPrefab, y calcula él mismo la posición de spawn.
        inventory.RPC_RequestDiscard(itemKey);

        // Feedback inmediato: vaciar el slot de origen sin esperar el round-trip.
        // El estado final igual lo dicta el server via IsDirty -> Refresh(); si el
        // server rechaza el descarte, su ResyncClient() vuelve a dibujar el item.
        if (dragged.originalParent != null)
        {
            dragged.originalParent.GetComponent<ItemContainerSlot>()?.ClearReference();
            dragged.originalParent.GetComponent<CraftingDropSlot>()?.ClearReference();
        }

        // Destroy está diferido al final del frame (antes del render), así que el
        // OnEndDrag que corre después de este OnDrop es inofensivo: a lo sumo
        // re-parenta un objeto moribundo que nunca llega a dibujarse.
        Destroy(dragged.gameObject);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        return true;
    }
}
