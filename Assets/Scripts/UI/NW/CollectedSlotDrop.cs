using UnityEngine;
using UnityEngine.EventSystems;

public class CollectedSlotDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag.GetComponent<ItemSlotDrag>();
        if (dragged == null) return;

        // Evitar si el slot ya está ocupado
        if (transform.childCount > 0) return;

        // Mover el item al slot de collected
        dragged.transform.SetParent(transform);
        dragged.transform.localPosition = Vector3.zero;

        // Aquí se podría notificar al inventario que ya no está equipado
        // Ej: EquipManager.Instance.UnequipItem(dragged.GetItemData());
    }
}

