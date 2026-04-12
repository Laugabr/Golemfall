using UnityEngine;
using Fusion;

public class DestructibleObject : NetworkBehaviour, IDamageable
{
    [Header("Feedback")]
    [SerializeField] private HitFeedback hitFeedback;
    [SerializeField] private GameObject breakEffectPrefab;

    [Header("Drop (opcional)")]
    [SerializeField] private NetworkObject dropPrefab;

    [Header("Rewards (opcional)")]
    [SerializeField] private int experienceReward = 0;
    [SerializeField] private GameEventType trackEvent = GameEventType.BreakBreakable;

    public void TakeDamage(int amount, GameObject source)
    {
        if (!Object.HasStateAuthority) return;

        if (hitFeedback != null)
            hitFeedback.FlashHit();

        if (breakEffectPrefab != null)
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);

        if (dropPrefab != null)
            Runner.Spawn(dropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        if (experienceReward > 0)
            BasicEventsManager.OnExperienceGain?.Invoke(experienceReward);

        TrackEvents.OnTrackEvent?.Invoke(trackEvent, 1);

        Runner.Despawn(Object);
    }
}