using Fusion;
using UnityEngine;

/// <summary>
/// Punto débil del boss. Es UN SOLO prefab (no se spawnea uno por punto):
/// el boss lo instancia una única vez y, mediante Setup(), le pasa los
/// puntos del mapa donde puede ubicarse. Cada moveInterval segundos se
/// teletransporta a uno de esos puntos (sin repetir el actual si hay
/// más de uno disponible).
///
/// Requiere un NetworkTransform (u otro componente de sync de posición)
/// en el mismo prefab para que el movimiento se replique a todos los
/// clientes correctamente.
/// </summary>
public class BossWeakPoint : NetworkBehaviour, IDamageable
{
    [SerializeField] private BossHealth bossHealth;

    [Header("Movimiento")]
    [Tooltip("Cada cuánto tiempo (segundos) el weak point cambia de lugar")]
    [SerializeField] private float moveInterval = 10f;

    // Puntos posibles, asignados en runtime vía Setup() porque el prefab
    // (asset) no puede referenciar Transforms de la escena directamente.
    private Transform[] weakPointPositions;

    [Networked] private int currentPointIndex { get; set; } = -1;
    [Networked] private TickTimer moveTimer { get; set; }

    /// <summary>
    /// Llamado por BossAttackHandler justo después de spawnear el prefab,
    /// para indicarle entre qué puntos del mapa puede moverse.
    /// </summary>
    public void Setup(Transform[] points)
    {
        weakPointPositions = points;

        if (!Object.HasStateAuthority) return;
        if (weakPointPositions == null || weakPointPositions.Length == 0)
        {
            Debug.LogWarning("[WeakPoint] Setup() llamado sin puntos asignados");
            return;
        }

        MoveToRandomPoint();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (weakPointPositions == null || weakPointPositions.Length == 0) return;

        if (moveTimer.ExpiredOrNotRunning(Runner))
        {
            MoveToRandomPoint();
        }
    }

    void MoveToRandomPoint()
    {
        int newIndex = currentPointIndex;

        if (weakPointPositions.Length > 1)
        {
            // Evita elegir el mismo punto dos veces seguidas
            while (newIndex == currentPointIndex)
                newIndex = Random.Range(0, weakPointPositions.Length);
        }
        else
        {
            newIndex = 0;
        }

        var target = weakPointPositions[newIndex];
        if (target == null)
        {
            // Si ese punto está mal asignado, reprogramamos el intento
            // pronto en vez de dejar al weak point trabado ahí.
            moveTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
            return;
        }

        currentPointIndex = newIndex;
        transform.position = target.position;
        transform.rotation = target.rotation;

        moveTimer = TickTimer.CreateFromSeconds(Runner, moveInterval);

        Debug.Log($"[WeakPoint] Se movió al punto {newIndex} ({target.name})");
    }

    public void TakeDamage(int amount, GameObject source)
    {
        Debug.Log("[WeakPoint] Damage recibido");

        bossHealth.TakeDamage(amount, source);
    }
}