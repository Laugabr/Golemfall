using UnityEngine;
using UnityEngine.EventSystems;

public class EquipSlotUI : MonoBehaviour, IDropHandler
{
    public int equipIndex = 0; // 0 o 1

    public void OnDrop(PointerEventData eventData)
    {
        if (DragData.item != null)
        {
            ReceiveDrop(DragData.item);
        }
    }

    public void ReceiveDrop(ItemData item)
    {
        // Buscar índice real del item en inventory
        int inventoryIndex = InventoryManager.Instance.FindIndex(item);
        if (inventoryIndex >= 0)
            EquipManager.Instance.EquipFromInventory(inventoryIndex, equipIndex);
    }
}

