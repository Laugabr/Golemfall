using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    public ItemData itemData;
    private bool playerInRange = false;
    [HideInInspector] public Vector3 originalPosition;

    void Awake()
    {
        originalPosition = transform.position;
    }

    void Reset()
    {
        // asegurarse que collider sea trigger
        Collider c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            InteractPrompt.Instance?.Show(transform, "F");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            InteractPrompt.Instance?.Hide();
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            TryPickup();
        }
    }
    public void ReturnToOriginalPosition()
    {
        transform.position = originalPosition;
        gameObject.SetActive(true);
    }


    public void TryPickup()
    {
        if (itemData == null) return;

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("No InventoryManager in scene.");
            return;
        }

        bool added = InventoryManager.Instance.AddItem(itemData);
        InteractPrompt.Instance?.Hide();

        if (added)
        {
            MessageManager.Instance?.Show("Item recolectado");
            Destroy(gameObject);
        }
        else
        {
            MessageManager.Instance?.Show("Inventario lleno");
        }
    }
}
