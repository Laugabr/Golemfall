using System.Collections.Generic;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using System;

public class NetworkInventory : NetworkBehaviour
{
    [Networked]
    public NetworkBool IsDirty { get; set; }

    // Inventario REAL del servidor
    public readonly List<string> Items = new List<string>();
    public readonly List<string> EquipedItems = new List<string>();

    private const string ITEMDATA_PATH = "DataSO/StatsData/Itemscrafteados/";

    #region Networking 

    #region Server 

    public void Server_AddItem(string itemID)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("Server_AddItem llamado sin autoridad (esto no debería pasar)");
            return;
        }
        
        if (CheckItemExistence(itemID))
        { 
            Items.Add(itemID);
            Debug.Log("Item " + itemID + " added to items list");

        }
        else
        {
            Debug.LogError( itemID + " not found in 'Items crafteados' folder");
            return;
        }
        IsDirty = true;

        Debug.Log($"[SERVER] Item agregado al inventario: {itemID}");

    }

    // Gets called by the cient by dragging an item to an equipment slot
    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_ServerEquipmentRequest(string itemID, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;

        var itemToEquip = Items.FirstOrDefault(item => item == itemID);

        if (itemToEquip == null)
        {
            Debug.LogError("Item not found in inventory");
            return;

        }
        else
        {
            InventoryEventsManager.OnItemEquiped?.Invoke(itemID);
            //RPC_ClientHUDUpdate(); //borrar el comentario antes de la entrega, luuuu aca iria la parte donde se le actualiza en la ui el inventario te dejo esa parte ;))

            EquipedItems.Add(itemToEquip); //Adds the first found
        }
    }
    

    // Gets called by the cient by dragging an item out of an equipment slot
    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_ServerUnequipmentRequest(string itemID, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;

        var itemToUnequip = EquipedItems.FirstOrDefault(item => item == itemID);

        if (itemToUnequip == null)
        {
            Debug.LogError("Item not found in inventory");
            return;
        }
        else
        {
            InventoryEventsManager.OnItemUnequiped?.Invoke(itemID);
            //RPC_ClientHUDUpdate(); //borrar el comentario antes de la entrega, luuuu aca iria la parte donde se le actualiza en la ui el inventario te dejo esa parte ;))

            EquipedItems.Remove(itemToUnequip);
        }
    }
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_ServerUneentRequest(string itemID, RpcInfo info = default)
    {
        //Runner.Spawn()
        //Aca queda por si en algun momento llegamos a hacer que puedas dropear los items 
    }
#endregion
#endregion


    private bool CheckItemExistence(string itemID)
    {
        var itemData = Resources.Load<ItemData>(ITEMDATA_PATH + itemID);
        
        var itemStatsData = Resources.Load<Stats>(ITEMDATA_PATH + "Stats_" + itemID);

        if (itemData == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
       

    
}


// Perdon pero lo pongo en el mismo script jaja

public static class InventoryEventsManager
{
    public static Action<string> OnItemEquiped;
    public static Action<string> OnItemUnequiped;

}