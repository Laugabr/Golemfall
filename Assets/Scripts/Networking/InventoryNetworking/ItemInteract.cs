using Fusion;
using UnityEngine;

// Handles player interaction with items and networked pickup
[RequireComponent(typeof(Collider), typeof(NetworkObject))]
public class ItemInteract : NetworkBehaviour
{
    [SerializeField] private ItemData itemData;
    private readonly KeyCode interactKey = KeyCode.F;

    private NetworkRunner runner;
    private bool localPlayerInRange = false;

    private NetworkObject localPlayerNO;
    private NetworkInventory localInventory;

  #region Networking 

    #region Server 
    // CLIENT → SERVER RPC to request item pickup
    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
    private void RPC_ServerRequestPickup(NetworkObject playerInventoryNO, RpcInfo info = default)
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
        //inv.Server_AddItem(itemData.id);
        Runner.Despawn(Object);
    }

#endregion

#endregion
    // Called when object spawns in the scene
    public override void Spawned()
    {
        Debug.Log(name + " Initialized in scene" );
    }

    private void Awake() 
    {
        runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
            Debug.LogError(" No se encontró un NetworkRunner en la escena.");
    }

    // Detect player entering item trigger
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var otherNO = other.GetComponent<NetworkObject>();
        if (otherNO == null) return;

        // Only set local player data if this is the local client
        if (otherNO.InputAuthority == runner.LocalPlayer)
        {
            localPlayerInRange = true;
            localPlayerNO = otherNO;
            localInventory = otherNO.GetComponent<NetworkInventory>();

            InteractPrompt.Instance?.Show(transform, "F");
        }
    }

    // Detect player leaving item trigger
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var otherNO = other.GetComponent<NetworkObject>();
        if (otherNO == null) return;

        // Clear local player data and hide UI
        if (otherNO.InputAuthority == runner.LocalPlayer)
        {
            localPlayerInRange = false;
            localPlayerNO = null;
            localInventory = null;

            InteractPrompt.Instance?.Hide();
        }
    }

    private void Update()
    {
        if (!localPlayerInRange) return;
        if (localInventory == null) return;

        // Check for player input to pick up item
        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log(" CLIENTE LOCAL → pidiendo pickup");

            // Send player's NetworkObject to server to pick up item
            RPC_ServerRequestPickup(localInventory.Object);
        }
    }


}

