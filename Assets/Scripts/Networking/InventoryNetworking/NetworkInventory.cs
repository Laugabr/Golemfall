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
    private NetworkRunner runner;

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
            ItemData data = Resources.Load<ItemData>("DataSO/StatsData/Itemscrafteados/" + itemID);
            if (data != null)
            {
                InventoryManager.Instance.AddItem(data);
                Debug.Log("[NetworkInventory] Also forwarded to InventoryManager: " + data.displayName);
            }
            else
            {
                Debug.LogError("[NetworkInventory] ItemData NOT FOUND for ID: " + itemID);
            }

        }
        else
        {
            Debug.LogError(itemID + " not found in 'Items crafteados' folder");
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
        Debug.Log("Equipment request made ");

        var itemToEquip = Items.FirstOrDefault(item => item == itemID);

        if (itemToEquip == null)
        {
            Debug.LogError("Item not found in inventory");
            return;

        }
        else
        {
            var playStats = GetComponent<PlayerStats>();

            if(playStats == null)return;
            else
            {
                playStats.RPC_EquipItem(itemID); //Adds it to the stats  SERVER -> SERVER
                //RPC_ClientHUDUpdate(); //borrar el comentario antes de la entrega, luuuu aca iria la parte donde se le actualiza en la ui el inventario te dejo esa parte ;))
                Debug.Log("Equipment succesful ");
                EquipedItems.Add(itemToEquip); //Adds the first found
            }
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
            var playStats = GetComponent<PlayerStats>();

            if(playStats == null)return;
            else
            {
                playStats.RPC_UnequipItem(itemID); //Removes it from the stats SERVER -> SERVER
                //RPC_ClientHUDUpdate(); //borrar el comentario antes de la entrega, luuuu aca iria la parte donde se le actualiza en la ui el inventario te dejo esa parte ;)) SERVER -> CLIENT
                Debug.Log("Equipment succesful ");
                EquipedItems.Remove(itemToUnequip); //Adds the first found
            }
        }
    }
    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_ServerDropRequest(string itemID, RpcInfo info = default)
    {
        //Runner.Spawn()
        //Aca queda por si en algun momento llegamos a hacer que puedas dropear los items 
    }
    #endregion
    #endregion

    private void Awake()
    {

    }

    //Checks in assets if item exist 
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!HasInputAuthority) return;
            foreach (var id in Items)
            {   

                Debug.Log(id);

                RPC_ServerEquipmentRequest(id);
            }

            Debug.Log($"[{Object.InputAuthority}] Equipado TODO");
        }

        if (Input.GetKeyDown(KeyCode. U))
        {
            var playStats = GetComponent<PlayerStats>();

            foreach (var player in runner.ActivePlayers)
            {   
                string s = $"[{Object.InputAuthority}] STATS: ";
                foreach (var st in playStats.localStats)
                s += $"{st.statType}={st.statValue} ";

                Debug.Log(player.PlayerId + s);
            }
        }
    }
    //Called when
    public override void Spawned()
    {
            runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
            Debug.LogError(" No se encontró un NetworkRunner en la escena.");
    }
}
    


// Perdon pero lo pongo en el mismo script jaja

public static class InventoryEventsManager
{
    public static Action<string> OnItemEquiped;
    public static Action<string> OnItemUnequiped;

}