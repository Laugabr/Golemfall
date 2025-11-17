using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(NetworkObject))]
public class ItemInteract : NetworkBehaviour
{
    [SerializeField] private ItemData itemData;
    private readonly KeyCode interactKey = KeyCode.F;
    private bool localPlayerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var netw_obj = other.GetComponent<NetworkObject>();
        if (netw_obj == null) return;

        // Si este collider pertenece al player LOCAL -> mostramos prompt solo localmente
        if (netw_obj.InputAuthority == Runner.LocalPlayer)
        {
            localPlayerInRange = true;
            InteractPrompt.Instance?.Show(transform, interactKey.ToString());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var netw_obj = other.GetComponent<NetworkObject>();
        if (netw_obj == null) return;

        if (netw_obj.InputAuthority == Runner.LocalPlayer)
        {
            localPlayerInRange = false;
            InteractPrompt.Instance?.Hide();
        }
    }

    private void Update()
    {
        // Solo el cliente local chequea la tecla
        if (!localPlayerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            // Llamamos al RPC que el servidor recibirá (target = StateAuthority)
            RPC_RequestPickup();
        }
    }

    // Cliente -> Servidor
    // Usamos RpcSources.All para permitir que el cliente invoque aun cuando el item NO tenga input authority
    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
    private void RPC_RequestPickup(RpcInfo info = default)
    {
        // info.Source es el PlayerRef del que envió el RPC
        var requestingPlayer = info.Source;

        Debug.Log($"[ItemInteract] RPC_RequestPickup recibido en server. Item: {(itemData != null ? itemData.id : "NO_ID")}, solicitado por {requestingPlayer}");

        // Aquí se valida (distancia, si ya fue tomado, espacio en inventario...) y luego:
        // - Agregar al inventario server-side
        // - Runner.Despawn(Object) o Object.SetActive(false)
        // - Enviar confirmación al cliente  (RPC desde StateAuthority -> InputAuthority)
    }

    // Opcional: helper para el server cuando confirme
    [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.InputAuthority)]
    private void RPC_ConfirmPickup(RpcInfo info = default, int pickedItemId = -1)
    {
        // Esto correrá SOLO en el cliente que pidió (InputAuthority)
        Debug.Log($"[ItemInteract] RPC_ConfirmPickup recibido en cliente. ItemId: {pickedItemId}");
        InteractPrompt.Instance?.Hide();
        // Aquí el cliente puede actualizar UI local (o llamar a PlayerInventoryLocal.AddItem)
    }
}
