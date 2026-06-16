using UnityEngine;
using Fusion;

/// <summary>
/// Agregá este componente a cualquier DestructibleObject que deba desactivar
/// una puerta/barrera al romperse.
/// 
/// Funciona suscribiéndose al evento OnDestroyed de DestructibleObject.
/// Para que funcione en el cliente, usa NetworkVFXManager.RPC_DisableDoor()
/// que garantiza que el RPC llegue aunque el DestructibleObject ya se haya despawneado.
/// </summary>
public class OnDestroyUnlockCollider : MonoBehaviour
{
    [Tooltip("Puertas con NetworkDoor que se desactivan al romper este objeto.")]
    [SerializeField] private NetworkDoor[] doorsToDisable;

    private void Awake()
    {
        var destructible = GetComponent<DestructibleObject>();
        if (destructible != null)
            destructible.OnDestroyed += HandleDestroyed;
    }

    private void HandleDestroyed()
    {
        foreach (var door in doorsToDisable)
        {
            if (door == null) continue;

            // Usamos NetworkVFXManager para mandar el RPC desde un objeto permanente.
            // Esto garantiza que el cliente reciba el mensaje aunque el DestructibleObject
            // ya se haya despawneado en el momento que el RPC se procesa.
            if (NetworkVFXManager.Instance != null)
                NetworkVFXManager.Instance.RPC_DisableDoor(door.DoorId);
            else
                door.Disable(); // fallback local si no hay manager
        }
    }
}