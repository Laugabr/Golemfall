using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkInventory : NetworkBehaviour
{
    [Networked]
    public NetworkBool IsDirty { get; set; }

    // Inventario REAL del servidor
    public readonly List<string> items = new List<string>();

    public void Server_AddItem(string itemID)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("⚠ Server_AddItem llamado sin autoridad (esto no debería pasar)");
            return;
        }

        items.Add(itemID);
        IsDirty = true;

        Debug.Log($"[SERVER] Item agregado al inventario: {itemID}");
    }
}



