using Fusion;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

public class NetworkInventory : NetworkBehaviour
{
    [Networked] public NetworkBool IsDirty { get; set; }

    [Networked, Capacity(12)]
    public NetworkLinkedList<InventorySlot> Items => default;

    public List<InventorySlot> LocalItems;

    [Networked, Capacity(3)]
    public NetworkLinkedList<short> EquippedItems => default;

    #region SERVER

    public bool AddItem_Server(short itemKey, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority)
            return false;

        if (Items.Count >= 12)
        {
            Debug.Log("[SERVER] full inventory");
            return false;
        }

        if (ItemData.GetItem(itemKey) == null)
        {
            Debug.Log("[SERVER] invalid item");
            return false;
        }

        Items.Add(InventorySlot.Create(null, itemKey));
        IsDirty = true;

        RPC_UpdateLocalInventory();

        Debug.Log($"[SERVER] Item added ({Items.Count}/12)");
        return true;
    }

    #endregion

    #region CLIENT

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_UpdateLocalInventory(RpcInfo info = default)
    {
        LocalItems = new List<InventorySlot>(Items);
    }

    #endregion

    #region EQUIP

       [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestEquip(short itemKey)
    {
        if (!Object.HasStateAuthority) return;

        // 1. Validar si tiene el item
        if (!Items.Any(s => s.itemKey == itemKey)) return;

        // 2. Equipar
        if (!EquippedItems.Contains(itemKey))
        {
            EquippedItems.Add(itemKey);
            
            // 3. Notificar a las stats
            GetComponent<PlayerStats>().RefreshStats(); 
            IsDirty = true;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestUnequip(short itemKey, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;

        if (EquippedItems.Remove(itemKey))
        {
            GetComponent<PlayerStats>().RefreshStats();
            IsDirty = true;
        }
    }

    #endregion
}

// Static event manager for inventory actions
public static class InventoryEventsManager
{
    public static System.Action<string> OnItemEquiped;
    public static System.Action<string> OnItemUnequiped;

}

// Serializable inventory slot to share data through ntwork
[System.Serializable]
public struct InventorySlot : INetworkStruct
{
    public short itemKey;

    public readonly ItemData GetItem() => ItemData.GetItem(itemKey);

    public ItemData GetData()
    {
        if (ResourcesManager.instance == null || ResourcesManager.instance.inventoryItemBank == null)
            return null;
            
        return ResourcesManager.instance.inventoryItemBank.GetValue<ItemData>(itemKey);
    }
    
    
    // Parameter: either the item data or the item id 

    public static InventorySlot Create(ItemData item = null, short id = -1)
    {
        short finalKey = id;

        // Si pasamos el objeto, usamos el banco para obtener su ID
        if (item != null && ResourcesManager.instance != null)
        {
            finalKey = ResourcesManager.instance.inventoryItemBank.GetKey(item);
        }

        return new InventorySlot { itemKey = finalKey };
    }


    public readonly bool IsItem(ItemData item)
    {
        return item != null && ItemData.GetKey(item) == itemKey;
    }
}