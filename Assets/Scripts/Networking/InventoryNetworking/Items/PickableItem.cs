using Fusion;
using UnityEngine;

//Wrapper for the item itself
public class PickableItem : NetworkBehaviour
{
    [SerializeField] private ItemData itemData;
    [Networked] public int Count { get; set; } = 1;
    private NetworkRunner runner;
    private bool localPlayerInRange = false;

    private NetworkObject localPlayerNO;
    public ItemData Item => itemData;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_Collect(NetworkObject playerInventoryNO, RpcInfo info = default)
    {   
        Debug.Log(" SERVER: RPC_RequestPickup recibido");       
        
        if (playerInventoryNO == null)
        {
            Debug.LogError(" playerInventoryNO vino NULL");
            return;
        }

        // Get player's NetworkInventory component
        var inv = playerInventoryNO.GetComponent<NetworkInventory>();
        if (inv == null)
        {
            Debug.LogError(" No se encontró NetworkInventory en playerInventoryNO");
            return;
        }

        // Add item to player's inventory and despawn item
        short itemKey = ItemData.GetKey(itemData);
        
        if(inv.AddItem_Server(itemKey))
        {
            Runner.Despawn(Object);
        }
    }
    
    private void Awake() 
    {
        runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
            Debug.LogError(" No se encontró un NetworkRunner en la escena.");
    }
}