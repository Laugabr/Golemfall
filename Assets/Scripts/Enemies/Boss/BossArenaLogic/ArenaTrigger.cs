using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger de entrada a la arena del boss.
/// Cuando todos los players registrados en PlayerRegistry están dentro,
/// activa el BossAI directamente.
///
/// Setup en escena:
///   - Este componente va en un GameObject con un Collider trigger
///     que cubra la entrada/interior de la arena.
///   - Asignar la referencia a BossAI en el inspector.
/// </summary>
public class ArenaTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI bossAI;

    [Header("Settings")]
    [Tooltip("Si true, el trigger se desactiva después de activar el boss (evita retriggering)")]
    [SerializeField] private bool disableAfterActivation = true;

    private HashSet<Transform> playersInside = new();
    private bool hasActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Usamos el root para evitar múltiples colliders del mismo player
        Transform root = other.transform.root;
        playersInside.Add(root);

        Debug.Log($"[Arena] Entra: {other.name} ({playersInside.Count}/{PlayerRegistry.Players.Count})");

        CheckAllInside();
    }

    private void OnTriggerExit(Collider other)
    {
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
