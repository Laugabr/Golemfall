using Fusion;
using UnityEngine;

/// <summary>
/// Puerta/barrera que cierra la arena del boss una vez que todos los
/// players están dentro, para evitar que entren o salgan players nuevos
/// durante la pelea.
///
/// Es un prefab: BossAI lo spawnea una sola vez (lazy, vía
/// InitializeArenaGate()) y controla su estado llamando a Close()/Open()
/// en cada activación/desactivación del boss.
///
/// Setup en escena/prefab:
///   - Este componente va en el prefab que se asigna en
///     BossAI → Arena Gate → Arena Gate Prefab.
///   - Asignar (opcional) el collider que bloquea el paso y/o el
///     GameObject visual de la puerta cerrada.
/// </summary>
public class ArenaGate : NetworkBehaviour
{
    [Header("Bloqueo")]
    [Tooltip("Collider que se activa para bloquear el paso una vez cerrada la puerta")]
    [SerializeField] private Collider blockingCollider;

    [Header("Visual (opcional)")]
    [Tooltip("GameObject visual que se muestra cuando la puerta está cerrada (ej. modelo de reja)")]
    [SerializeField] private GameObject closedVisual;

    [Networked] private bool isClosed { get; set; }

    public bool IsClosed => isClosed;

    public override void Spawned()
    {
        ApplyVisualState(isClosed);
    }

    public override void Render()
    {
        ApplyVisualState(isClosed);
    }

    /// <summary>
    /// Cierra la arena. Llamado por ArenaTrigger cuando todos los players
    /// están dentro.
    /// </summary>
    public void Close()
    {
        if (!Object.HasStateAuthority) return;
        if (isClosed) return;

        isClosed = true;
        Debug.Log("[ArenaGate] Arena cerrada");
    }

    /// <summary>
    /// Reabre la arena. Pensado para llamarse cuando el boss muere
    /// (ej. desde BossAI.DisableBoss() o ArenaRespawnManager.DeactivateArena()).
    /// </summary>
    public void Open()
    {
        if (!Object.HasStateAuthority) return;
        if (!isClosed) return;

        isClosed = false;
        Debug.Log("[ArenaGate] Arena reabierta");
    }

    void ApplyVisualState(bool closed)
    {
        if (blockingCollider != null)
            blockingCollider.enabled = closed;

        if (closedVisual != null)
            closedVisual.SetActive(closed);
    }
}
