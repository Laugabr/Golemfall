using UnityEngine;
using Fusion;

/// <summary>
/// Pico de suelo spawneado por BossAttackHandler.
/// Daña al player al contacto y se destruye después de lifeTime segundos.
/// No necesita cambios respecto al original.
/// </summary>
public class GroundSpike : NetworkBehaviour
{
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float lifeTime = 2f;

    private DealDamage dealDamage;

    public override void Spawned()
    {
        dealDamage = GetComponent<DealDamage>();

        if (dealDamage != null)
            dealDamage.SetAttacker(transform);

        Invoke(nameof(Despawn), lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (other.CompareTag("Player"))
        {
            Debug.Log("[GroundSpike] Hit player");
            other.GetComponent<IDamageable>()?.TakeDamage(damageAmount, gameObject); // Aplica da�o al player
        }

    }

    void Despawn()
    {
        if (Object != null && Object.IsValid)
            Runner.Despawn(Object);
    }
}