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
    [SerializeField] public BossHealth bossHealth;

    [Header("Movimiento")]
    [Tooltip("Cada cuánto tiempo (segundos) el weak point cambia de lugar")]
    [SerializeField] private float moveInterval = 10f;

    [Header("Desactivación")]
    [Tooltip("Raíz de los visuals a ocultar cuando se desactiva (ej. modelo, VFX). " +
             "Si se deja vacío, se usan todos los Renderer hijos.")]
    [SerializeField] private GameObject visualsRoot;
    [Tooltip("Collider de hit del weak point. Si se deja vacío, se busca con GetComponent.")]
    [SerializeField] private Collider hitCollider;

    // Puntos posibles, asignados en runtime vía Setup() porque el prefab
    // (asset) no puede referenciar Transforms de la escena directamente.
    private Transform[] weakPointPositions;

    [Networked] private int currentPointIndex { get; set; } = -1;
    [Networked] private TickTimer moveTimer { get; set; }

    /// <summary>
    /// Estado networked: true mientras el weak point está operativo
    /// (se mueve y puede recibir daño). Se pone en false cuando el
    /// boss muere — ver Deactivate(). OnChangedRender corre en todos
    /// los peers para sincronizar visuals/collider.
    /// </summary>
    [Networked, OnChangedRender(nameof(OnActiveChanged))]
    private NetworkBool IsActive { get; set; }

    public bool IsWeakPointActive => IsActive;

    void Awake()
    {
        bossHealth = GetComponentInParent<BossHealth>();

        if (hitCollider == null)
            hitCollider = GetComponent<Collider>();
    }

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

        Activate();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!IsActive) return;
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
        // Si ya está desactivado (boss muerto), ignoramos el daño —
        // evita que algún hit "atrasado" en vuelo siga golpeando al
        // boss después de que la pelea terminó.
        if (!IsActive) return;

        Debug.Log("[WeakPoint] Damage recibido");

        bossHealth.TakeDamage(amount, source);
    }

    /// <summary>
    /// Apaga el weak point: deja de moverse, deja de recibir daño,
    /// y se oculta visualmente en todos los peers. Llamado desde
    /// BossAttackHandler cuando el boss muere (ver BossAI.DisableBoss()).
    /// No destruye/despawnea la instancia — mismo patrón que el resto
    /// del sistema (pool reutilizable), por si el boss vuelve a pelear.
    /// </summary>
    public void Deactivate()
    {
        if (!Object.HasStateAuthority) return;
        if (!IsActive) return;

        IsActive = false;
        Debug.Log("[WeakPoint] Desactivado — boss derrotado");
    }

    /// <summary>
    /// Reactiva el weak point para una nueva pelea (ej. si el boss
    /// se puede re-pelear desde cero). Vuelve a elegir un punto al azar.
    /// </summary>
    public void Activate()
    {
        if (!Object.HasStateAuthority) return;
        if (weakPointPositions == null || weakPointPositions.Length == 0) return;

        IsActive = true;
        MoveToRandomPoint();
    }

    /// <summary>
    /// Corre en todos los peers cuando IsActive cambia (incluida la
    /// reconciliación inicial al spawnear). Sincroniza visuals/collider.
    /// </summary>
    private void OnActiveChanged()
    {
        if (hitCollider != null)
            hitCollider.enabled = IsActive;

        if (visualsRoot != null)
        {
            visualsRoot.SetActive(IsActive);
        }
        else
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.enabled = IsActive;
        }
    }
}