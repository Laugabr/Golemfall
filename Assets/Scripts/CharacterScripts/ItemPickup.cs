using UnityEngine;
/*
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    public ItemData itemData;
    private bool pickedUp = false;
    [HideInInspector] public Vector3 originalPosition;

    void Awake()
    {
        originalPosition = transform.position;
    }

    void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    public void ReturnToOriginalPosition()
    {
        transform.position = originalPosition;
        gameObject.SetActive(true);
        pickedUp = false;
    }

    public void TryPickup()
    {
        if (pickedUp) return;
        pickedUp = true;

        if (itemData == null) return;

        bool added = InventoryManager.Instance.AddItem(itemData);
        InteractPrompt.Instance?.Hide();

        if (added)
        {
            MessageManager.Instance?.Show("Item recolectado");
            Destroy(gameObject);
        }
    }
}
*/