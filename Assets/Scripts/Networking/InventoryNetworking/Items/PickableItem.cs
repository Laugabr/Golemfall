using Fusion;
using UnityEngine;

//Wrapper for the item itself
public class PickableItem : NetworkBehaviour
{
    [SerializeField] private ItemData itemData;
    [Networked] public int Count { get; set; } = 1;

    public ItemData Item => itemData;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_Collect(RpcInfo info = default)
    {
        if(!Object.HasStateAuthority) return;
        var playerObj = Runner.GetPlayerObject(info.Source);
        if (playerObj == null) return;

        var inventory = playerObj.GetComponent<NetworkInventory>();
        if (inventory == null) return;

        inventory.AddItem_Server(ItemData.GetKey(itemData));
        Runner.Despawn(Object);
    }
    
    

}