using UnityEngine;
using Fusion;

/// <summary>
/// Diente que cae desde el techo.
/// Al impactar con el suelo o un player:
///   - Aplica da�o si golpea a un player.
///   - Se resetea a su posici�n original (ceilingOrigin) despu�s de un delay,
///     listo para volver a caer en el pr�ximo ataque.
///
/// No se destruye: se "oculta" y vuelve al techo para reusarse.
/// El FallingTeethSpawner controla cu�ndo vuelve a caer.
///
/// Alternativa: si prefer�s que el objeto lo despawnee el Spawner y
/// lo respawnee como NetworkObject nuevo, reemplaz� Reset() por
/// Runner.Despawn(Object) y dej� que FallingTeethSpawner maneje el pool.
/// </summary>
public class FallingTeeth : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float fallSpeed = 10f;

    [Header("Reset")]
    [Tooltip("Tiempo que espera en el suelo antes de volver al techo")]
    [SerializeField] private float resetDelay = 2f;

    // Posici�n inicial (techo), guardada al spawnear
    private Vector3 ceilingOrigin;
    [SerializeField] int damageAmount = 10;
    private DealDamage dealDamage;

    [Networked] private bool isFalling { get; set; }
    [Networked] private bool hasHit { get; set; }

    public override void Spawned()
    {
        dealDamage = GetComponent<DealDamage>();
        if (dealDamage != null)
            dealDamage.SetAttacker(transform);

        // Guardamos la posici�n desde donde fue spawneado (= techo)
        ceilingOrigin = transform.position;

        if (Object.HasStateAuthority)
        {
            isFalling = true;
            hasHit = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!isFalling) return;
        if (hasHit) return;

        transform.position += Vector3.down * fallSpeed * Runner.DeltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (hasHit) return;
        if (!isFalling) return;

        bool hitPlayer = other.CompareTag("Player");
        bool hitGround = other.CompareTag("Ground");
        var damageable = other.GetComponent<IDamageable>();

        if (!hitPlayer && !hitGround) return;

        hasHit = true;
        isFalling = false;

        Debug.Log($"[FallingTooth] Impacto con {other.name}");

        if (hitPlayer)
            damageable?.TakeDamage(damageAmount, gameObject); // Aplica da�o al player

        // Inicia el ciclo de regreso al techo
        Invoke(nameof(ResetToOrigin), resetDelay);
    }

    /// <summary>
    /// Vuelve el diente a su posici�n original en el techo.
    /// Queda est�tico hasta que BossAttackHandler lo vuelva a activar
    /// con un nuevo spawn del telegraph.
    ///
    /// NOTA: si este objeto es spawneado una vez y reutilizado, ya est� listo.
    /// Si prefer�s despawnear/respawnear, reemplaz� esta l�gica por
    /// Runner.Despawn(Object) y dej� el pool en el Spawner.
    /// </summary>
    void ResetToOrigin()
    {
        if (!Object.HasStateAuthority) return;

        transform.position = ceilingOrigin;
        hasHit = false;

        // isFalling queda en false: el pr�ximo SpawnFallingTeeth
        // spawnea un telegraph que al completarse dispara otro tooth.
        // Si reutiliz�s este objeto en lugar de spawnear uno nuevo,
        // llam� a StartFalling() desde el spawner en ese momento.
        Debug.Log("[FallingTooth] Reseteado al techo");
    }

    /// <summary>
    /// Llama esto desde el spawner si reutiliz�s el mismo objeto
    /// en lugar de spawnear uno nuevo cada vez.
    /// </summary>
    public void StartFalling()
    {
        if (!Object.HasStateAuthority) return;

        transform.position = ceilingOrigin;
        hasHit = false;
        isFalling = true;

        Debug.Log("[FallingTooth] Comienza a caer");
    }
}