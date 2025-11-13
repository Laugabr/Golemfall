using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int slotIndex;
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

    // Rechaza drops entrantes si el slot está ocupado (no permitimos swaps)
    public void OnDrop(PointerEventData eventData)
    {
        // Si lo que se está soltando viene desde un equip slot, el EquipSlotUI ya lo maneja en su OnEndDrag.
        // Aquí solo prevenimos que otro sistema ponga algo encima de un slot ocupado.
        var draggedItem = DragData.item;
        if (draggedItem == null) return;

        // Si el slot ya tiene item, rechazamos el drop
        if (InventoryManager.Instance.GetItemAt(slotIndex) != null)
        {
            Debug.Log("[SlotUI] Slot ocupado. No se puede soltar aquí.");
            // Aseguramos que la visual vuelva al origen si existe
            if (DragData.sourceSlot != null) DragData.sourceSlot.RefreshSlot();
            return;
        }
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

