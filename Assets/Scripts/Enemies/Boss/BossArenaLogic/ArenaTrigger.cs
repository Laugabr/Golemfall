using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Trigger de entrada a la arena del boss.
/// En cuanto el PRIMER player entra, se teletransporta automáticamente a
/// todo el resto del grupo (registrado en PlayerRegistry) adentro de la
/// arena, sin importar dónde estén — ya no hace falta que cada uno camine
/// hasta acá. Una vez que están todos (vía teleport), activa el BossAI.
///
/// Nota: el cierre/apertura de la puerta de la arena (ArenaGate) NO se
/// maneja acá. Vive en BossAI, atado a cada ActivateBoss()/ResetBoss(),
/// para que se cierre tanto en la primera activación como en cada
/// reintento tras un wipe — este trigger solo dispara una vez.
///
/// Requiere NetworkObject en el mismo GameObject (aunque no tenga estado
/// networked propio) para poder chequear HasStateAuthority y así evitar
/// que la lógica corra en clientes que no son el host — los colliders de
/// esta zona existen localmente en todas las máquinas, así que sin este
/// chequeo OnTriggerEnter se ejecutaría también en clientes.
///
/// Setup en escena:
///   - Este componente va en un GameObject con un Collider trigger
///     que cubra la entrada/interior de la arena, y un NetworkObject.
///   - Asignar BossAI y ArenaRespawnManager (este último para el teleport
///     del resto del grupo) en el inspector.
/// </summary>
public class ArenaTrigger : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI bossAI;
    [SerializeField] private ArenaRespawnManager respawnManager;

    [Header("Settings")]
    [Tooltip("Si true, el trigger se desactiva después de activar el boss (evita retriggering)")]
    [SerializeField] private bool disableAfterActivation = true;

    private HashSet<Transform> playersInside = new();
    private bool hasActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        // Solo el host procesa la activación de la arena — evita logs/ejecución
        // duplicada en clientes (los colliders de esta zona existen localmente
        // en todas las máquinas, así que sin este check el evento físico
        // dispararía igual en cada una).
        if (!Object.HasStateAuthority) return;
        if (hasActivated) return;
        if (!other.CompareTag("Player")) return;

        // Usamos el root para evitar múltiples colliders del mismo player
        Transform root = other.transform.root;
        playersInside.Add(root);

        Debug.Log($"[Arena] Entra: {other.name} → trayendo al resto del grupo");

        // Nuevo diseño: en vez de esperar a que cada player camine hasta
        // acá, en cuanto entra el primero traemos a todo el resto.
        if (respawnManager != null)
            respawnManager.TeleportPlayersIntoArena(root);
        else
            Debug.LogWarning("[Arena] ArenaRespawnManager no asignado — no se puede teletransportar al resto del grupo");

        // Los marcamos como "dentro" directamente: el teleport es networked
        // y puede tardar uno o más ticks en reflejarse físicamente, así que
        // no dependemos del OnTriggerEnter de cada uno para confirmarlo.
        foreach (var player in PlayerRegistry.Players)
        {
            if (player != null)
                playersInside.Add(player);
        }

        CheckAllInside();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (!other.CompareTag("Player")) return;

        Transform root = other.transform.root;
        playersInside.Remove(root);

        Debug.Log($"[Arena] Sale: {other.name}");
    }

    void CheckAllInside()
    {
        if (hasActivated) return;

        int totalPlayers = PlayerRegistry.Players.Count;
        int inside = playersInside.Count;

        if (totalPlayers <= 0) return;
        if (inside < totalPlayers) return;

        Debug.Log("[Arena] TODOS DENTRO → activando boss");
        hasActivated = true;

        if (bossAI != null)
            bossAI.ActivateBoss();
        else
            Debug.LogError("[Arena] BossAI no asignado en el inspector");

        if (disableAfterActivation)
            gameObject.SetActive(false);
    }
}