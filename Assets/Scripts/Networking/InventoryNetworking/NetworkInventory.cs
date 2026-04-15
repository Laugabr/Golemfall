using Fusion;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class NetworkInventory : NetworkBehaviour
{
    // IsDirty ya no es [Networked] — es local, se setea via RPC
    public bool IsDirty { get; set; }

    [Networked, Capacity(12)]
    public NetworkLinkedList<InventorySlot> Items => default;

    public List<InventorySlot> LocalItems;

    [Networked, Capacity(3)]
    public NetworkLinkedList<short> EquippedItems => default;

    #region SERVER

    public bool AddItem_Server(short itemKey, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return false;

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
        RPC_UpdateLocalInventory();
        RPC_NotifyInventoryChanged();

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

    // Notifica al cliente que el inventario cambió — se setea una sola vez, no en loop
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_NotifyInventoryChanged(RpcInfo info = default)
    {
        IsDirty = true;
    }

    #endregion

    #region EQUIP

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestEquip(short itemKey)
    {
        if (!Object.HasStateAuthority) return;

        if (!Items.Any(s => s.itemKey == itemKey)) return;

        if (!EquippedItems.Contains(itemKey))
        {
            Debug.Log($"[SERVER] Item equipped: {itemKey}");
            EquippedItems.Add(itemKey);
            GetComponent<PlayerStats>().RefreshStats();
            RPC_NotifyInventoryChanged();
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
            RPC_NotifyInventoryChanged();
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

    public static InventorySlot Create(ItemData item = null, short id = -1)
    {
        short finalKey = id;
        if (item != null && ResourcesManager.instance != null)
            finalKey = ResourcesManager.instance.inventoryItemBank.GetKey(item);
        return new InventorySlot { itemKey = finalKey };
    }

    public readonly bool IsItem(ItemData item)
    {
        return item != null && ItemData.GetKey(item) == itemKey;
    }
}