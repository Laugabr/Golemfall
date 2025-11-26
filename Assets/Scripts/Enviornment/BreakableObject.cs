using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakableObject : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private int hitsToBreak = 3;
    [SerializeField] private GameObject breakEffectPrefab;
    [SerializeField] private int experienceReward = 1;
    [SerializeField] private HitFeedback hitFeedback;

    private int currentHits = 0;
    private bool isBroken = false;

    public void ReceiveHit()
    {

    }
    public void Break()
    {
        isBroken = true;

        if (breakEffectPrefab) Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);

        // Buscar automáticamente BreakableDrop en el mismo objeto
        BreakableDrop drop = GetComponent<BreakableDrop>();
        if (drop != null) drop.SpawnDrop(transform.position);

        BasicEventsManager.OnExperienceGain?.Invoke(experienceReward);
        Debug.Log($"{gameObject.name} se rompió. +{experienceReward} EXP");
        Destroy(gameObject);

    }
}
