using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Handles dragging UI items between slots
public class ItemSlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    public Transform originalParent {get; set;}
    private Vector2 originalPos;

    private void Awake()
    {
        // Ensure CanvasGroup exists for controlling raycast blocking
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
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
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == UIRoot.Instance.dragLayer)
        {
        // Return to original parent if not dropped on a valid slot
            transform.SetParent(originalParent);
            transform.localPosition = originalPos;
            originalParent = transform.parent;

        }
    }
    
    // Returns the ID of the item in this slot
    public string GetItemID() 
    { 
        var slot = GetComponent<ItemSlot>(); 
        if (slot == null) return ""; 
        return slot.GetItemID(); 
    }
}
