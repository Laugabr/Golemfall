using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ejecuta los ataques físicos del boss.
///
/// Ground Spikes:
///   Elige N puntos aleatorios de groundPoints (Fisher-Yates).
///   Fase 1 → menos picos; Fase 2 → más picos.
///   Cada punto primero muestra un TelegraphZone y luego spawnea el spike.
///   Estos sí se spawnean/despawnean por ataque (GroundSpike se autodestruye
///   por lifeTime, eso está bien).
///
/// Falling Teeth:
///   A diferencia de los picos, los dientes son un POOL FIJO: se instancian
///   UNA SOLA VEZ (uno por ceilingPoint) cuando el boss se activa, y nunca
///   se despawnean. Cada ataque simplemente le ordena a los dientes ya
///   existentes que vuelvan a caer (StartFalling()), en lugar de crear
///   instancias nuevas. Esto evita que el mapa se llene de dientes
///   acumulados, ya que FallingTeeth.cs está diseñado para resetearse y
///   reutilizarse, no para destruirse.
/// </summary>
public class BossAttackHandler : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NetworkObject groundSpikePrefab;
    [SerializeField] private NetworkObject fallingToothPrefab;
    [SerializeField] private NetworkObject telegraphPrefab;
    [SerializeField] private NetworkObject weakPointPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] groundPoints;
    [SerializeField] private Transform[] ceilingPoints;

    [Header("Weak Point — puntos donde puede aparecer")]
    [Tooltip("Los puntos del mapa entre los que el weak point se mueve (3 recomendado)")]
    [SerializeField] private Transform[] weakPointPositions;

    // Instancia única del weak point. Igual que con los dientes, se
    // spawnea una sola vez y nunca se vuelve a instanciar: simplemente
    // se mueve entre weakPointPositions (eso lo maneja BossWeakPoint).
    private NetworkObject weakPointInstance;

    // Pool de dientes ya instanciados, uno por ceilingPoint.
    // Se llena una sola vez en InitializeTeethPool().
    private List<FallingTeeth> teethPool = new();
    private bool teethPoolInitialized = false;

    [Header("Telegraph")]
    [SerializeField] private float telegraphTime = 1.5f;

    [Header("Ground Spikes — cantidad por fase")]
    [Tooltip("Cuántos puntos activa en fase 1")]
    [SerializeField] private int phase1SpikeCount = 3;
    [Tooltip("Cuántos puntos activa en fase 2")]
    [SerializeField] private int phase2SpikeCount = 5;

    [Header("Falling Teeth — delay entre dientes")]
    [Tooltip("Delay entre cada diente en fase 1")]
    [SerializeField] private float phase1ToothDelay = 0.4f;
    [Tooltip("Delay entre cada diente en fase 2")]
    [SerializeField] private float phase2ToothDelay = 0.2f;

    // =============================
    // SUELO
    // =============================

    public void SpawnGroundSpikesRandom(int phase)
    {
        if (!Object.HasStateAuthority) return;
        if (groundPoints == null || groundPoints.Length == 0) return;

        int count = Mathf.Min(
            phase == 2 ? phase2SpikeCount : phase1SpikeCount,
            groundPoints.Length
        );

        int[] indices = ShuffledIndices(groundPoints.Length);

        for (int i = 0; i < count; i++)
        {
            var point = groundPoints[indices[i]];
            if (point == null) continue;

            Vector3 pos = point.position;

            var telegraphObj = Runner.Spawn(telegraphPrefab, pos, Quaternion.identity);
            var telegraph = telegraphObj.GetComponent<TelegraphZone>();
            telegraph.Init(telegraphTime, () => Runner.Spawn(groundSpikePrefab, pos, Quaternion.identity));
        }
    }

    // =============================
    // TECHO
    // =============================

    /// <summary>
    /// Instancia el pool de dientes una sola vez, uno por ceilingPoint.
    /// Se llama automáticamente la primera vez que se necesita el ataque.
    /// Los dientes quedan estáticos en el techo (isFalling = false) hasta
    /// que SpawnFallingTeeth() los active.
    /// </summary>
    void InitializeTeethPool()
    {
        if (teethPoolInitialized) return;
        if (!Object.HasStateAuthority) return;
        if (ceilingPoints == null || ceilingPoints.Length == 0) return;

        foreach (var point in ceilingPoints)
        {
            if (point == null) continue;

            var obj = Runner.Spawn(fallingToothPrefab, point.position, Quaternion.identity);
            var tooth = obj.GetComponent<FallingTeeth>();

            if (tooth != null)
                teethPool.Add(tooth);
            else
                Debug.LogWarning("[BossAttackHandler] fallingToothPrefab sin componente FallingTeeth");
        }

        teethPoolInitialized = true;
        Debug.Log($"[BossAttackHandler] Pool de dientes inicializado: {teethPool.Count} dientes");
    }

    /// <summary>
    /// Hace caer los dientes ya existentes en el pool, en orden aleatorio
    /// y con delays escalonados. NO instancia objetos nuevos — reusa el
    /// mismo pool siempre, por eso el conteo de dientes en el mapa nunca crece.
    /// </summary>
    public void SpawnFallingTeeth(int phase)
    {
        if (!Object.HasStateAuthority) return;

        InitializeTeethPool();
        if (teethPool.Count == 0) return;

        float delay = phase == 2 ? phase2ToothDelay : phase1ToothDelay;
        int[] indices = ShuffledIndices(teethPool.Count);

        StartCoroutine(DropTeethSequence(indices, delay));
    }

    IEnumerator DropTeethSequence(int[] indices, float delayBetween)
    {
        foreach (int i in indices)
        {
            if (!Object.HasStateAuthority) yield break;

            var tooth = teethPool[i];

            // Si el diente está null (despawneado externamente) o todavía
            // está cayendo/en cooldown de un ataque anterior, lo saltamos
            // en vez de generar uno nuevo.
            if (tooth == null) continue;

            var telegraphObj = Runner.Spawn(telegraphPrefab, tooth.transform.position, Quaternion.identity);
            var telegraph = telegraphObj.GetComponent<TelegraphZone>();
            telegraph.Init(telegraphTime, () =>
            {
                if (tooth != null)
                    tooth.StartFalling();
            });

            yield return new WaitForSeconds(Random.Range(delayBetween, delayBetween * 2f));
        }
    }

    // =============================
    // WEAK POINT
    // =============================

    /// <summary>
    /// Instancia el weak point una sola vez (igual patrón que el pool de
    /// dientes) y le pasa los puntos del mapa entre los que puede moverse.
    /// Llamar al activar/resetear el boss. Si ya existe, no hace nada.
    /// </summary>
    public void InitializeWeakPoint()
    {
        if (!Object.HasStateAuthority) return;
        if (weakPointInstance != null) return;
        if (weakPointPrefab == null)
        {
            Debug.LogWarning("[BossAttackHandler] weakPointPrefab no asignado");
            return;
        }
        if (weakPointPositions == null || weakPointPositions.Length == 0)
        {
            Debug.LogWarning("[BossAttackHandler] weakPointPositions vacío");
            return;
        }

        Vector3 startPos = weakPointPositions[0].position;
        weakPointInstance = Runner.Spawn(weakPointPrefab, startPos, Quaternion.identity);

        var weakPoint = weakPointInstance.GetComponent<BossWeakPoint>();
            if (weakPoint != null){
                weakPoint.Setup(weakPointPositions);
                weakPoint.bossHealth = GetComponent<BossHealth>();}
        else
            Debug.LogWarning("[BossAttackHandler] weakPointPrefab sin componente BossWeakPoint");

        Debug.Log($"[BossAttackHandler] Weak point inicializado con {weakPointPositions.Length} puntos posibles");
    }

    // =============================
    // UTILIDAD
    // =============================

    int[] ShuffledIndices(int length)
    {
        int[] indices = new int[length];
        for (int i = 0; i < length; i++) indices[i] = i;
        for (int i = length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return indices;
    }
}