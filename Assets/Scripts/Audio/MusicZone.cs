using Fusion;
using UnityEngine;

/// <summary>
/// Zona de música: un trigger que colocás en el mapa. Cuando el JUGADOR LOCAL
/// entra, le pide al MusicDirector que ponga su pista (fight, ambience, etc.);
/// al salir, la quita. Reutilizable: cada zona elige su MusicTrack en el Inspector.
///
/// Multiplayer: filtramos por HasInputAuthority para reaccionar SOLO al jugador
/// local de este cliente. Así cada jugador escucha la música de la zona en la que
/// está él, sin importar dónde estén los demás.
///
/// Requiere un Collider marcado como "Is Trigger" en este objeto.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MusicZone : MonoBehaviour
{
    [Tooltip("Pista que suena mientras el jugador local está dentro de la zona.")]
    [SerializeField] private MusicTrack track;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        MusicDirector.Instance?.PushZone(track);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        MusicDirector.Instance?.PopZone(track);
    }

    /// <summary>
    /// True solo si el collider es del jugador local (el que tiene InputAuthority
    /// en este cliente). El NetworkObject puede estar en un padre del collider,
    /// por eso usamos GetComponentInParent.
    /// </summary>
    private static bool IsLocalPlayer(Collider other)
    {
        if (!other.CompareTag("Player")) return false;
        var netObj = other.GetComponentInParent<NetworkObject>();
        return netObj != null && netObj.HasInputAuthority;
    }
}
