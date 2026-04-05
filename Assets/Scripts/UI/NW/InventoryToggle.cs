using UnityEngine;
using UnityEngine.EventSystems;
using Fusion;
using UnityEngine.UI;

public class InventoryToggle : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner == null || !runner.IsRunning) return;

            if (inventoryPanel != null)
            {
                bool nowActive = !inventoryPanel.activeSelf;
                inventoryPanel.SetActive(nowActive);
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
}