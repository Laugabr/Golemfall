using UnityEngine;
using UnityEngine.EventSystems;

public class CraftingDropSlot : MonoBehaviour, IDropHandler
{
    public CraftingUI craftingUI;

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag?.GetComponent<ItemSlotDrag>();
        if (dragged == null) return;

        if (transform.childCount > 0) return;

        var itemSlot = dragged.GetComponent<ItemSlot>();
        if (itemSlot == null) return;

        // Mover visual
        dragged.transform.SetParent(transform);
        dragged.transform.localPosition = Vector3.zero;

        // Notificar al sistema
        craftingUI.OnItemPlaced();
    }
}