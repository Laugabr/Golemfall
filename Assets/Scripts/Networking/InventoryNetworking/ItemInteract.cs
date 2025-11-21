using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(NetworkObject))]
public class ItemInteract : NetworkBehaviour
{
    [SerializeField] private ItemData itemData;
    private readonly KeyCode interactKey = KeyCode.F;

    private NetworkRunner runner;
    private bool localPlayerInRange = false;

    private PlayerRef localPlayerRef;

    #region RPC SERVER

    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
    private void RPC_ServerRequestPickup(PlayerRef requestingPlayer, RpcInfo info = default)
    {
        Debug.Log("SERVER: RPC_ServerRequestPickup recibido");

        // Obtener el NetworkObject del jugador
        var playerObj = Runner.GetPlayerObject(requestingPlayer);
        if (playerObj == null)
        {
            Debug.LogError("SERVER: No se encontró NetworkObject del jugador");
            return;
        }

        var inv = playerObj.GetComponent<NetworkInventory>();
        if (inv == null)
        {
            Debug.LogError("SERVER: Este jugador no tiene NetworkInventory");
            return;
        }

        // Agregar ítem
        inv.Server_AddItem(itemData.id);

        // Despawnear el item
        Runner.Despawn(Object);
    }

    #endregion

    public override void Spawned()
    {
        Debug.Log($"{name} initialized in scene");
    }

    private void Awake()
    {
        runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
            Debug.LogError("No se encontró un NetworkRunner en la escena.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var otherNO = other.GetComponent<NetworkObject>();
        if (otherNO == null) return;

        // Sólo si es el jugador local
        if (otherNO.InputAuthority == runner.LocalPlayer)
        {
            localPlayerInRange = true;
            localPlayerRef = otherNO.InputAuthority;

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

            InteractPrompt.Instance?.Hide();
        }
    }

    private void Update()
    {
        if (!localPlayerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log("CLIENTE LOCAL → enviando pedido de pickup al server");

            // 🔥 Ahora sólo mandás PlayerRef
            RPC_ServerRequestPickup(runner.LocalPlayer);
        }
    }
}