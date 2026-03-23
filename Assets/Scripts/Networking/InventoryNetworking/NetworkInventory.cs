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

        // See if has item
        if (!Items.Any(s => s.itemKey == itemKey)) return;

        // Equip it
        if (!EquippedItems.Contains(itemKey))
        {
            Debug.Log($"[SERVER] Item equipped: {itemKey}");

            EquippedItems.Add(itemKey);
            
            // Refresh stats
            GetComponent<PlayerStats>().RefreshStats(); 
            IsDirty = true;
        }
        else
        {
            Debug.Log($"[SERVER] Item already equipped: {itemKey}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestUnequip(short itemKey, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;

        if (EquippedItems.Remove(itemKey))
        {
            Debug.Log($"[SERVER] Item unequipped: {itemKey}");

            GetComponent<PlayerStats>().RefreshStats();
            IsDirty = true;
        }
        else
        {
            Debug.Log($"[SERVER] Item not equipped: {itemKey}");
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

    //Gets ItemData using the itemKey, using ResourceManager's inventoryItemBank
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

        // if parameter is ItemData, get key using GetKey()
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