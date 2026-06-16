using UnityEngine;

/// <summary>
/// Identificador de puerta/barrera en la escena.
/// Se adjunta al objeto que actúa como puerta (con la niebla como hijo).
/// El ID único permite que NetworkVFXManager.RPC_DisableDoor() identifique
/// qué puerta desactivar en todos los peers cuando se manda desde el servidor.
/// </summary>
public class NetworkDoor : MonoBehaviour
{
    [Tooltip("ID único para esta puerta. Debe ser diferente para cada puerta en la escena.")]
    [SerializeField] private int doorId;
    public int DoorId => doorId;

    /// <summary>
    /// Desactiva el objeto entero — incluyendo todos sus hijos (niebla, collider, etc).
    /// Llamado por NetworkVFXManager.RPC_DisableDoor() en todos los peers.
    /// </summary>
    public void Disable()
    {
        gameObject.SetActive(false);
    }
}