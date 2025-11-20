using UnityEngine;

public class InventoryDebug : MonoBehaviour
{
    [SerializeField] NetworkInventory inv;

    private void Update()
    {
        if (inv != null && inv.IsDirty)
        {
            inv.IsDirty = false;
            Debug.Log("INVENTARIO ACTUALIZADO:");
            foreach (var it in inv.Items)
                Debug.Log(" - " + it);
        }
    }
}

