using Fusion;
using UnityEngine;

public class InstantKillZone : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private bool killPlayers = true;
    [SerializeField] private bool killEnemies = true;

    public  void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        if (!killPlayers && other.CompareTag("Player")) return;
        if (!killEnemies && other.CompareTag("Enemy")) return;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;

        damageable.TakeDamage(999999, gameObject);
    }
}