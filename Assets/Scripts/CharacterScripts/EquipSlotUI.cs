using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class EquipSlotUI : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int equipIndex = 0;
    private Canvas canvas;
    private GameObject dragIcon;
    private Image dragImage;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (DragData.item != null)
            ReceiveDrop(DragData.item);
    }

    public void ReceiveDrop(ItemData item)
    {
        // Buscar índice real del item en el inventario y equiparlo
        int inventoryIndex = InventoryManager.Instance.FindIndex(item);
        if (inventoryIndex >= 0)
            EquipManager.Instance.EquipFromInventory(inventoryIndex, equipIndex);
    }

    // NUEVO: comenzar a arrastrar desde un slot equipado
    public void OnBeginDrag(PointerEventData eventData)
    {
        ItemData it = EquipManager.Instance.equipped[equipIndex];
        if (it == null) return;

        DragData.item = it;
        DragData.sourceSlot = null; // viene desde equip, no inventory

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragImage = dragIcon.AddComponent<Image>();
        dragImage.raycastTarget = false;
        dragImage.sprite = it.icon;
        RectTransform rt = dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64, 64);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            dragIcon.transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bool droppedOnInventory = false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var res in results)
        {
            // Ver si se soltó sobre un SlotUI del inventario
            var invSlot = res.gameObject.GetComponentInParent<SlotUI>();
            if (invSlot != null)
            {
                // Si el slot está vacío, desequipamos
                if (InventoryManager.Instance.GetItemAt(invSlot.slotIndex) == null)
                {
                    EquipManager.Instance.UnEquipToInventory(equipIndex);
                    droppedOnInventory = true;
                    InventoryUI ui = FindObjectOfType<InventoryUI>();
                    ui.Refresh();       
                }
                else
                {
                    // Si hay algo, hacemos intercambio
                    int inventoryIndex = invSlot.slotIndex;
                    ItemData invItem = InventoryManager.Instance.GetItemAt(inventoryIndex);
                    if (invItem != null)
                    {
                        // swap: equipar el del inventario y desequipar el actual
                        EquipManager.Instance.EquipFromInventory(inventoryIndex, equipIndex);
                        InventoryManager.Instance.AddOrSpawn(invItem);
                        droppedOnInventory = true;
                    }
                }
                break;
            }
        }

        if (!droppedOnInventory)
        {
            // Si no se soltó en ningún slot de inventario, simplemente cancelamos
        }

        if (dragIcon != null) Destroy(dragIcon);
        DragData.item = null;
    }
}

