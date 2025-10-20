using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int slotIndex; // 0..4
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

        DragData.item = it;
        DragData.sourceSlot = this;

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
        bool droppedOnEquip = false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var res in results)
        {
            var equip = res.gameObject.GetComponentInParent<EquipSlotUI>();
            if (equip != null)
            {
                equip.ReceiveDrop(DragData.item); // enviamos referencia directa
                droppedOnEquip = true;
                break;
            }
        }

        //si no se soltó sobre un equip slot válido, devolver al slot original
        if (!droppedOnEquip && DragData.sourceSlot != null)
        {
            // Volvemos a colocar el ítem visualmente en el slot original
            DragData.sourceSlot.RefreshSlot();
        }

        if (dragIcon != null) Destroy(dragIcon);

        // Limpiamos datos estáticos
        DragData.item = null;
        DragData.sourceSlot = null;
    }


    public void RefreshSlot()
    {
        // actualizamos el sprite según el item actual en InventoryManager
        ItemData it = InventoryManager.Instance.GetItemAt(slotIndex);
        Image img = GetComponent<Image>();
        if (img != null)
        {
            if (it != null)
            {
                img.sprite = it.icon;
                img.color = Color.white;
            }
            else
            {
                img.sprite = null;
                img.color = new Color(1, 1, 1, 0.2f);
            }
        }
    }
}

