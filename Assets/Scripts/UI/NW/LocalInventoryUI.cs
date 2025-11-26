using UnityEngine;

public class LocalInventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;

    public void ToggleInventory()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }

    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);
    }
}
