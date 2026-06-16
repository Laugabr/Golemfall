using UnityEngine;
using Fusion;

/// <summary>
/// Objeto destructible en red. Solo el StateAuthority procesa el daño.
/// Usa NetworkVFXManager para mandar el VFX de ruptura a todos los peers
/// desde un objeto permanente, garantizando que llegue aunque este objeto
/// ya se haya despawneado cuando el cliente procese el mensaje.
/// </summary>
public class DestructibleObject : NetworkBehaviour, IDamageable
{
    [Header("Feedback")]
    [SerializeField] private HitFeedback hitFeedback;

    [Header("Drop (opcional)")]
    [SerializeField] private NetworkObject dropPrefab;

    [Header("Rewards (opcional)")]
    [SerializeField] private int experienceReward = 0;
    [SerializeField] private GameEventType trackEvent = GameEventType.BreakBreakable;

    public event System.Action OnDestroyed;

    public void TakeDamage(int amount, GameObject source)
    {
        if (!Object.HasStateAuthority) return;

        if (hitFeedback != null)
            hitFeedback.FlashHit();

        // VFX de ruptura via NetworkVFXManager — llega al cliente aunque
        // este objeto ya se haya despawneado cuando el RPC se procese.
        if (NetworkVFXManager.Instance != null)
            NetworkVFXManager.Instance.RPC_SpawnDestructibleBreakVFX(transform.position);

        if (dropPrefab != null)
            Runner.Spawn(dropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        if (experienceReward > 0)
            BasicEventsManager.OnExperienceGain?.Invoke(experienceReward);

        TrackEvents.OnTrackEvent?.Invoke(trackEvent, 1);

        // Disparamos el evento local en el host antes del despawn.
        // El cliente recibe la notificación via RPC_DisableDoor en OnDestroyUnlockCollider.
        OnDestroyed?.Invoke();
        Runner.Despawn(Object);
    }
}