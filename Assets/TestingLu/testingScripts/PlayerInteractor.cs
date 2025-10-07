using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private GameObject inventoryPanel; // panel del inventario (debe estar desactivado al inicio)
    [SerializeField] private Camera mainCamera;

    private bool isInventoryOpen = false;
    private bool canPickup = false;
    private ItemPickup nearbyItem;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        HandleInteraction();
        HandleInventoryToggle();
    }

    private void HandleInteraction()
    {
        // recoger item si presiona F
        if (canPickup && nearbyItem != null && Input.GetKeyDown(KeyCode.F))
        {
            nearbyItem.TryPickup();
            nearbyItem = null;
            canPickup = false;
        }
    }

    private void HandleInventoryToggle()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            isInventoryOpen = !isInventoryOpen;
            inventoryPanel.SetActive(isInventoryOpen);
            // manejar el cursor
            Cursor.visible = isInventoryOpen;
            Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ItemPickup pickup))
        {
            canPickup = true;
            nearbyItem = pickup;
            InteractPrompt.Instance?.Show(pickup.transform, "F");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out ItemPickup pickup) && pickup == nearbyItem)
        {
            canPickup = false;
            nearbyItem = null;
            InteractPrompt.Instance?.Hide();
        }
    }
}

