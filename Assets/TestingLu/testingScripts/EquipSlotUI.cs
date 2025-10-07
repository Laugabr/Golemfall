using UnityEngine;
using UnityEngine.EventSystems;

public class EquipSlotUI : MonoBehaviour, IDropHandler
{
    public int equipIndex = 0; // 0 o 1

    // Este método es llamado por EventSystem si implementás IDropHandler,
    // pero también hay un método público ReceiveDrop usado desde SlotUI para simplificar.
    public void OnDrop(PointerEventData eventData)
    {
        // si alguien usa IDropHandler directo, chequeamos DragData
        if (DragData.sourceInventoryIndex >= 0)
        {
            EquipManager.Instance.EquipFromInventory(DragData.sourceInventoryIndex, equipIndex);
        }
    }

    public void ReceiveDrop(int inventoryIndex)
    {
        EquipManager.Instance.EquipFromInventory(inventoryIndex, equipIndex);
    }
}
