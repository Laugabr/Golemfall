using UnityEngine;

public class ItemContainerSlot : MonoBehaviour
{
    [SerializeField] private ItemSlot currentItem;

    public bool IsEmpty => currentItem == null;

    public void AssignItem(ItemSlot item)
    {
        currentItem = item;
        // Ajustes para UI (RectTransform)
        var rt = item.transform as RectTransform;
        item.transform.SetParent(transform, false);
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    public void ClearSlot()
    {
        if (currentItem != null)
            Destroy(currentItem.gameObject);
        currentItem = null;
    }

    public ItemSlot GetItem()
    {
        return currentItem;
    }
}

