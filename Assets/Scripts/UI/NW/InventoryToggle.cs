using UnityEngine;
using UnityEngine.EventSystems;
using Fusion;
using UnityEngine.UI;

public class InventoryToggle : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;
    public bool IsInventoryOpen => inventoryPanel != null && inventoryPanel.activeSelf;

    private void Update()
    {
        // I: toggle (abrir/cerrar).
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
        // Escape: solo cierra si está abierto (no abre).
        else if (Input.GetKeyDown(KeyCode.Escape) && IsInventoryOpen)
        {
            Close();
        }
    }

    /// <summary>Cierra el panel del inventario. No hace nada si ya está cerrado.</summary>
    public void Close()
    {
        if (inventoryPanel == null) return;

        inventoryPanel.SetActive(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}