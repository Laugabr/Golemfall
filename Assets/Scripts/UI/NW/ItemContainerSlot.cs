using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ItemContainerSlot : MonoBehaviour
{
    [SerializeField] private InventorySlotUI currentItem; // <-- Usamos la clase que ya tenés

    public bool IsEmpty => currentItem == null;

    public void AssignItem(InventorySlotUI item)
    {
        currentItem = item;
        item.transform.SetParent(transform, false);
        item.transform.localPosition = Vector3.zero;
    }

    public void ClearSlot()
    {
        currentItem = null;
    }

    public InventorySlotUI GetItem()
    {
        return currentItem;
    }
}

