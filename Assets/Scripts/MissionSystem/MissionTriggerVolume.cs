using UnityEngine;
using Fusion;

/// <summary>
/// Volumen de trigger que arranca una misión por id cuando el primer jugador entra.
/// Solo actúa del lado del host (StateAuthority) y una sola vez.
///
/// Uso:
///  - Batalla05: poné este componente en el arco, missionToStart = "mision_batalla_05".
///  - Batalla04: poné uno en la zona del golemtruoso, missionToStart = "mision_batalla_04"
///    (arranca la misión aunque llegues fuera del hilo encadenado).
///
/// Requiere un Collider con isTrigger = true (Reset lo fuerza).
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionTriggerVolume : MonoBehaviour
{
    [Tooltip("Id (nombre del asset en Resources/DataSO/Missions) de la misión a arrancar.")]
    [SerializeField] private string missionToStart = "";

    private bool _fired;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag("Player")) return;

        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        // En hosted mode el host es StateAuthority de todos los players.
        // Esto garantiza que la lógica corra solo en el host.
        if (!netObj.HasStateAuthority) return;

        var controller = FindFirstObjectByType<MissionController>();
        if (controller == null) return;

        _fired = true;
        if (!string.IsNullOrEmpty(missionToStart))
            controller.StartMissionById(missionToStart);
    }
}