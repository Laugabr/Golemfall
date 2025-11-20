using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalPos;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPos = transform.localPosition;

        canvasGroup.blocksRaycasts = false; // permitir drop
        transform.SetParent(UIRoot.Instance.dragLayer); // capa superior para arrastrar
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == UIRoot.Instance.dragLayer)
        {
            // No cayó en ningún slot válido
            transform.SetParent(originalParent);
            transform.localPosition = originalPos;
        }
    }
}
