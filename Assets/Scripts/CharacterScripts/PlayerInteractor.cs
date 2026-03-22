using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private GameObject inventoryPanel; // panel del inventario (debe estar desactivado al inicio)
    [SerializeField] private Camera mainCamera;

    private bool canPickup = false;
    private PickableItem nearbyItem;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        HandleInteraction();
    }

    private void HandleInteraction()
    {
        // recoger item si presiona F
        if (canPickup && nearbyItem != null && Input.GetKeyDown(KeyCode.F))
        {
            nearbyItem = null;
            canPickup = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PickableItem pickup))
        {
            canPickup = true;
            nearbyItem = pickup;
            InteractPrompt.Instance?.Show(pickup.transform, "F");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PickableItem pickup) && pickup == nearbyItem)
        {
            canPickup = false;
            nearbyItem = null;
            InteractPrompt.Instance?.Hide();
        }
    }
}

