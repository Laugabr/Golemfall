using Fusion;
using UnityEngine;

public class PickableItem : NetworkBehaviour
{
    [SerializeField] private ItemData itemData;
    [Networked] public int Count { get; set; } = 1;
    [Networked] private Vector3 SpawnedPosition { get; set; }
    
    private NetworkRunner runner;
    private bool localPlayerInRange = false;
    private NetworkObject localPlayerNO;
    
    public ItemData Item => itemData;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            SpawnedPosition = transform.position;
        }
        else
        {
            transform.position = SpawnedPosition;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_Collect(NetworkObject playerInventoryNO, RpcInfo info = default)
    {   
        Debug.Log(" SERVER: RPC_RequestPickup recibido");       
        
        if (playerInventoryNO == null)
        {
            Debug.LogError(" playerInventoryNO vino NULL");
            return;
        }

        var inv = playerInventoryNO.GetComponent<NetworkInventory>();
        if (inv == null)
        {
            Debug.LogError(" No se encontró NetworkInventory en playerInventoryNO");
            return;
        }

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