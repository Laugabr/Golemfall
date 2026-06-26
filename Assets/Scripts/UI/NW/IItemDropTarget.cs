// Contrato de "lugar donde un item puede aterrizar".
// Implementado por EquipSlotDrop, CollectedSlotDrop y CraftingDropSlot.
//
// Existe para que la lógica de aterrizaje (equipar/desequipar en red, colocar,
// actualizar preview, etc.) viva en un solo lugar por slot y la usen por igual:
//   - el drag and drop (IDropHandler.OnDrop), y
//   - el quick-move por doble-click (ItemQuickMove).
//
// Así no hay caminos paralelos que mantener: ambos invocan el mismo TryAccept.
public interface IItemDropTarget
{
    // Intenta colocar el item arrastrado en este slot, aplicando sus efectos
    // (RPCs de red, AssignItem, preview...). Devuelve true si lo aceptó.
    bool TryAccept(ItemSlotDrag dragged);
}