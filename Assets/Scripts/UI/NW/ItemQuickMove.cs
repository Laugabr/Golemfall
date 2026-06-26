using UnityEngine;
using UnityEngine.EventSystems;

// Mueve un item por doble-click siguiendo el anillo:
//   Inventario -> Equip -> Craft -> Inventario
// Si el siguiente contenedor del anillo está lleno, salta al siguiente que
// tenga lugar. Si ninguno de los otros dos tiene lugar, no hace nada.
//
// No duplica lógica: reutiliza el mismo IItemDropTarget.TryAccept que usa el
// drag and drop, así un doble-click se comporta igual que arrastrar el item
// hasta ese slot (incluyendo los RPC de equip/unequip y el preview de craft).
//
// Va en el prefab del item (mismo GameObject que ItemSlotDrag).
[RequireComponent(typeof(ItemSlotDrag))]
public class ItemQuickMove : MonoBehaviour, IPointerClickHandler
{
    private enum Stage { Inventory, Equip, Craft }

    private ItemSlotDrag drag;
    private NwInventoryUI inventoryUI;
    private CraftingUI craftingUI;

    private void Awake()
    {
        drag = GetComponent<ItemSlotDrag>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (eventData.clickCount != 2) return;
        QuickMove();
    }

    private void QuickMove()
    {
        if (drag == null) return;

        Transform source = transform.parent;
        if (source == null) return;

        if (!TryGetStage(source, out Stage current)) return;

        EnsureRefs();

        // Recorre los otros dos contenedores en orden de anillo y se queda con
        // el primero que tenga un slot vacío (regla de salto).
        foreach (Stage candidate in RingAfter(current))
        {
            IItemDropTarget target = GetTargetFor(candidate);
            if (target == null) continue;

            // El destino lee originalParent para saber si el item venía de equip
            // (y desequiparlo). Lo seteamos como hace OnBeginDrag en el drag real.
            drag.originalParent = source;

            if (target.TryAccept(drag))
            {
                // Limpiar la referencia del origen, igual que OnEndDrag tras un
                // drop exitoso, para no dejar referencias colgadas.
                source.GetComponent<ItemContainerSlot>()?.ClearReference();
                source.GetComponent<CraftingDropSlot>()?.ClearReference();

                drag.originalParent = transform.parent;
            }
            return; // candidato con lugar encontrado: se intentó, no seguir el anillo
        }
        // Ningún otro contenedor tiene lugar: no se hace nada.
    }

    private bool TryGetStage(Transform slot, out Stage stage)
    {
        if (slot.GetComponent<CraftingDropSlot>() != null) { stage = Stage.Craft; return true; }
        if (slot.GetComponent<EquipSlotDrop>() != null)     { stage = Stage.Equip; return true; }
        if (slot.GetComponent<CollectedSlotDrop>() != null) { stage = Stage.Inventory; return true; }
        stage = Stage.Inventory;
        return false; // no está en un slot conocido (ej: en el dragLayer)
    }

    // Los otros dos stages del anillo, en orden, a partir del actual.
    private static Stage[] RingAfter(Stage s)
    {
        switch (s)
        {
            case Stage.Inventory: return new[] { Stage.Equip, Stage.Craft };
            case Stage.Equip:     return new[] { Stage.Craft, Stage.Inventory };
            default:              return new[] { Stage.Inventory, Stage.Equip }; // Craft
        }
    }

    private IItemDropTarget GetTargetFor(Stage stage)
    {
        switch (stage)
        {
            case Stage.Inventory:
                return inventoryUI != null
                    ? inventoryUI.GetFirstEmptyInventorySlot()?.GetComponent<IItemDropTarget>()
                    : null;
            case Stage.Equip:
                return inventoryUI != null
                    ? inventoryUI.GetFirstEmptyEquipSlot()?.GetComponent<IItemDropTarget>()
                    : null;
            case Stage.Craft:
                return craftingUI != null ? craftingUI.GetFirstEmptyCraftSlot() : null;
            default:
                return null;
        }
    }

    private void EnsureRefs()
    {
        if (inventoryUI == null) inventoryUI = FindFirstObjectByType<NwInventoryUI>();
        if (craftingUI == null) craftingUI = FindFirstObjectByType<CraftingUI>();
    }
}