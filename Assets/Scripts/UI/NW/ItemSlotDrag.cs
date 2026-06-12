using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Handles dragging UI items between slots
public class ItemSlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    public Transform originalParent { get; set; }
    private Vector2 originalPos;
    public static bool IsDragging { get; private set; }


    private void Awake()
    {
        // Ensure CanvasGroup exists for controlling raycast blocking
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        IsDragging = true;
        Debug.Log($"OnBeginDrag — UIRoot:{UIRoot.Instance != null} dragLayer:{UIRoot.Instance?.dragLayer != null}");
        if (UIRoot.Instance == null) return;
        originalParent = transform.parent;
        originalPos = transform.localPosition;

        canvasGroup.blocksRaycasts = false;
        transform.SetParent(UIRoot.Instance.dragLayer);
    }

    // Called every frame while dragging
    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;
        canvasGroup.blocksRaycasts = true;

        Debug.Log($"OnEndDrag — parent:{transform.parent?.name} dragLayer:{UIRoot.Instance.dragLayer?.name}");
        Debug.Log($"OnEndDrag — parentIsDragLayer:{transform.parent == UIRoot.Instance.dragLayer}");


        if (transform.parent == UIRoot.Instance.dragLayer)
        {
            // Drop falló: vuelve al original. Las referencias del origen no cambian.
            transform.SetParent(originalParent);
            transform.localPosition = originalPos;
        }
        else if (transform.parent != originalParent)
        {
            // Drop exitoso en otro container: limpiar la referencia del origen.
            // Sin esto, slots de inventario / crafteo conservan una referencia colgada
            // al item que ya se fue, causando crafts fantasma, slots que no aceptan drops,
            // y items destruidos cuando un Refresh limpia el slot anterior.
            if (originalParent != null)
            {
                originalParent.GetComponent<ItemContainerSlot>()?.ClearReference();
                originalParent.GetComponent<CraftingDropSlot>()?.ClearReference();
            }
        }

        originalParent = transform.parent;
    }

    // Returns the ID of the item in this slot
    public string GetItemID()
    {
        var slot = GetComponent<ItemSlot>();
        if (slot == null) return "";
        return slot.GetItemID();
    }
}