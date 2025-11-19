using Fusion;
using UnityEngine;

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
    // CLIENTE → SERVIDOR
    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
    private void RPC_ServerRequestPickup(NetworkObject playerInventoryNO, RpcInfo info = default)
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

        inv.Server_AddItem(itemData.id);
        Runner.Despawn(Object);
    }

#endregion

#endregion
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

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var otherNO = other.GetComponent<NetworkObject>();
        if (otherNO == null) return;

        if (otherNO.InputAuthority == runner.LocalPlayer)
        {
            localPlayerInRange = true;
            localPlayerNO = otherNO;
            localInventory = otherNO.GetComponent<NetworkInventory>();

            InteractPrompt.Instance?.Show(transform, "F");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var otherNO = other.GetComponent<NetworkObject>();
        if (otherNO == null) return;

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

        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log(" CLIENTE LOCAL → pidiendo pickup");

            // Enviamos el NetworkObject del inventario
            RPC_ServerRequestPickup(localInventory.Object);
        }
    }


}

