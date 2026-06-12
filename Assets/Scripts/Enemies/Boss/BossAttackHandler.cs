using UnityEngine;
using Fusion;
using System.Collections;

/// <summary>
/// Ejecuta los ataques físicos del boss.
///
/// Ground Spikes:
///   Elige N puntos aleatorios de groundPoints (Fisher-Yates).
///   Fase 1 → menos picos; Fase 2 → más picos.
///   Cada punto primero muestra un TelegraphZone y luego spawnea el spike.
///
/// Falling Teeth:
///   Lanza los dientes del techo en orden aleatorio con delays escalonados.
///   El diente se resetea al techo solo después de impactar (ver FallingTeeth.cs).
/// </summary>
public class BossAttackHandler : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NetworkObject groundSpikePrefab;
    [SerializeField] private NetworkObject fallingToothPrefab;
    [SerializeField] private NetworkObject telegraphPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] groundPoints;
    [SerializeField] private Transform[] ceilingPoints;

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

    public void SpawnFallingTeeth(int phase)
    {
        if (!Object.HasStateAuthority) return;
        if (ceilingPoints == null || ceilingPoints.Length == 0) return;

        float delay = phase == 2 ? phase2ToothDelay : phase1ToothDelay;
        int[] indices = ShuffledIndices(ceilingPoints.Length);

        StartCoroutine(SpawnTeethSequence(indices, delay));
    }

    IEnumerator SpawnTeethSequence(int[] indices, float delayBetween)
    {
        foreach (int i in indices)
        {
            var point = ceilingPoints[i];
            if (point == null) continue;

            if (!Object.HasStateAuthority) yield break;

            Vector3 pos = point.position;

            var telegraphObj = Runner.Spawn(telegraphPrefab, pos, Quaternion.identity);
            var telegraph = telegraphObj.GetComponent<TelegraphZone>();
            telegraph.Init(telegraphTime, () => Runner.Spawn(fallingToothPrefab, pos, Quaternion.identity));

            yield return new WaitForSeconds(Random.Range(delayBetween, delayBetween * 2f));
        }
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