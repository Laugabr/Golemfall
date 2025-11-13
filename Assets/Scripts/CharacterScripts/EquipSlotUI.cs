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
    private InventoryUI inventoryUI;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        inventoryUI = FindObjectOfType<InventoryUI>();
    }

    // Recibir drop desde inventario -> equip (solo si slot equip está vacío)
    public void OnDrop(PointerEventData eventData)
    {
        if (DragData.item != null)
            ReceiveDrop(DragData.item);
    }

    public void ReceiveDrop(ItemData item)
    {
        // Si el equip slot ya está ocupado, rechazamos el drop
        if (EquipManager.Instance == null) return;
        if (EquipManager.Instance.equipped == null) return;
        if (equipIndex < 0 || equipIndex >= EquipManager.Instance.equipped.Length) return;

        if (EquipManager.Instance.equipped[equipIndex] != null)
        {
            Debug.Log("[EquipSlotUI] Slot de equipamiento ocupado. No se puede equipar aquí.");
            return;
        }

        // Buscar índice real del item en el inventario y equiparlo
        int inventoryIndex = InventoryManager.Instance.FindIndex(item);
        if (inventoryIndex >= 0)
        {
            EquipManager.Instance.EquipFromInventory(inventoryIndex, equipIndex);
            if (inventoryUI != null) inventoryUI.Refresh();
        }
    }

    // Comenzar drag desde equip slot
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
        bool droppedOnInventorySlot = false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var res in results)
        {
            var invSlot = res.gameObject.GetComponentInParent<SlotUI>();
            if (invSlot != null)
            {
                // Solo permitimos desequipar si el slot del inventario está vacío
                if (InventoryManager.Instance.GetItemAt(invSlot.slotIndex) == null)
                {
                    EquipManager.Instance.UnEquipToInventory(equipIndex);
                    droppedOnInventorySlot = true;

                    if (inventoryUI != null)
                        inventoryUI.Refresh();
                }
                else
                {
                    // Slot ocupado: rechazamos el drop (no swap)
                    Debug.Log("[EquipSlotUI] Slot de inventario ocupado. No se puede desequipar aquí.");
                }

                break; // ya procesamos un slot
            }
        }

        // Si no se soltó en ningún slot de inventario válido, no hacemos nada: devolvemos el ítem al equip slot (visual intacta)
        if (dragIcon != null) Destroy(dragIcon);

        // Limpiamos datos estáticos
        DragData.item = null;
    }
}
