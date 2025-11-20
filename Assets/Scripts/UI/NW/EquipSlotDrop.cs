using UnityEngine;
using UnityEngine.EventSystems;

public class EquipSlotDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag.GetComponent<ItemSlotDrag>();
        if (dragged == null) return;

        // Evitar reemplazar si ya hay algo
        if (transform.childCount > 0) return;

        dragged.transform.SetParent(transform);
        dragged.transform.localPosition = Vector3.zero;

        // Aquí puedes avisar al inventario que este item quedó equipado
        // Ej: EquipManager.Instance.EquipItem(dragged.GetItemData());
    }
}
