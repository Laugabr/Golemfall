using UnityEngine;
using UnityEngine.EventSystems;

public class CraftingDropSlot : MonoBehaviour, IDropHandler
{
    public CraftingUI craftingUI;

    // Estado tipado del item actual. Evita que la lógica externa
    // tenga que mirar transform.childCount / GetChild(0) para saber
    // qué hay en el slot, lo que rompía el flujo si el prefab del slot
    // tenía cualquier child decorativo (background, highlight, VFX, etc).
    public ItemSlot CurrentItem { get; private set; }
    public bool IsEmpty => CurrentItem == null;

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag?.GetComponent<ItemSlotDrag>();
        if (dragged == null) return;

        if (!IsEmpty) return;

        var itemSlot = dragged.GetComponent<ItemSlot>();
        if (itemSlot == null) return;

        ItemSlot.PlaceInto(dragged.transform, transform);
        CurrentItem = itemSlot;

        craftingUI.OnItemPlaced();
    }

    public void Clear()
    {
        if (CurrentItem != null)
            Destroy(CurrentItem.gameObject);
        CurrentItem = null;
    }

    // Nullifica la referencia interna sin destruir el GameObject.
    // Llamado por ItemSlotDrag.OnEndDrag cuando el item se va de este slot
    // por drag exitoso a otro container. Distinto de Clear(), que sí
    // destruye porque el llamador quiere eliminar el item (ej: post-craft).
    public void ClearReference()
    {
        CurrentItem = null;
    }
}