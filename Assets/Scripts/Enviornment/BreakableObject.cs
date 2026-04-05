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
        if (isBroken) return;
        currentHits++;
        Debug.Log($"ReceiveHit llamado — {currentHits}/{hitsToBreak}");

        if (hitFeedback != null)
            hitFeedback.FlashHit();

        if (currentHits >= hitsToBreak)
            Break();
    }

    public void Break()
    {
        isBroken = true;

        if (breakEffectPrefab)
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);

        BreakableDrop drop = GetComponent<BreakableDrop>();
        if (drop != null) drop.SpawnDrop(transform.position);

        BasicEventsManager.OnExperienceGain?.Invoke(experienceReward);
        TrackEvents.OnTrackEvent?.Invoke(GameEventType.BreakBreakable, 1);
        Debug.Log($"{gameObject.name} se rompió. +{experienceReward} EXP");
        
        Destroy(gameObject);
    }
}