using Fusion;
using UnityEngine;

/// <summary>
/// Punto d�bil del boss. Es UN SOLO prefab (no se spawnea uno por punto):
/// el boss lo instancia una �nica vez y, mediante Setup(), le pasa los
/// puntos del mapa donde puede ubicarse. Cada moveInterval segundos se
/// teletransporta a uno de esos puntos (sin repetir el actual si hay
/// m�s de uno disponible).
///
/// Requiere un NetworkTransform (u otro componente de sync de posici�n)
/// en el mismo prefab para que el movimiento se replique a todos los
/// clientes correctamente.
/// </summary>
public class BossWeakPoint : NetworkBehaviour, IDamageable
{
    [SerializeField] public BossHealth bossHealth;

    [Header("Movimiento")]
    [Tooltip("Cada cu�nto tiempo (segundos) el weak point cambia de lugar")]
    [SerializeField] private float moveInterval = 10f;

    // Puntos posibles, asignados en runtime v�a Setup() porque el prefab
    // (asset) no puede referenciar Transforms de la escena directamente.
    private Transform[] weakPointPositions;

    [Networked] private int currentPointIndex { get; set; } = -1;
    [Networked] private TickTimer moveTimer { get; set; }


    void Awake()
    {
        bossHealth = GetComponentInParent<BossHealth>();
    }
    /// <summary>
    /// Llamado por BossAttackHandler justo despu�s de spawnear el prefab,
    /// para indicarle entre qu� puntos del mapa puede moverse.
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
            // Si ese punto est� mal asignado, reprogramamos el intento
            // pronto en vez de dejar al weak point trabado ah�.
            moveTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
            return;
        }

        currentPointIndex = newIndex;
        transform.position = target.position;
        transform.rotation = target.rotation;

        moveTimer = TickTimer.CreateFromSeconds(Runner, moveInterval);

        Debug.Log($"[WeakPoint] Se movi� al punto {newIndex} ({target.name})");
    }

    public void TakeDamage(int amount, GameObject source)
    {
        Debug.Log("[WeakPoint] Damage recibido");

        bossHealth.TakeDamage(amount, source);
    }
}