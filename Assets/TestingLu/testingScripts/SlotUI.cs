using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int slotIndex; // asigná 0..4 en inspector
    private Canvas canvas;
    private GameObject dragIcon;
    private Image dragImage;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ItemData it = InventoryManager.Instance.GetItemAt(slotIndex);
        if (it == null) return;

        DragData.sourceInventoryIndex = slotIndex;
        DragData.item = it;

        // crear icono drag
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
        if (dragIcon == null) return;
        dragIcon.transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null) Destroy(dragIcon);

        // raycast UI para ver si soltamos sobre equip slot
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        bool droppedOnEquip = false;
        foreach (var res in results)
        {
            var equip = res.gameObject.GetComponentInParent<EquipSlotUI>();
            if (equip != null)
            {
                // pedimos equip
                equip.ReceiveDrop(DragData.sourceInventoryIndex);
                droppedOnEquip = true;
                break;
            }
        }

        if (!droppedOnEquip)
        {
            // no se dropeó en equip => arrojamos el item al mundo como prototipo
            InventoryManager.Instance.ThrowItemFromInventory(DragData.sourceInventoryIndex);
        }

        // limpiar
        DragData.sourceInventoryIndex = -1;
        DragData.item = null;
    }
}

