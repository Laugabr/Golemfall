using UnityEngine;
using Fusion;

/// <summary>
/// Diente que cae desde el techo.
/// Al impactar con el suelo o un player:
///   - Aplica daño si golpea a un player.
///   - Vuelve a su posición original (ceilingOrigin) después de un delay,
///     y queda estático/inactivo hasta que BossAttackHandler lo reactive.
///
/// IMPORTANTE: este objeto se spawnea UNA SOLA VEZ por punto de techo
/// (ver BossAttackHandler.InitializeTeethPool) y se reutiliza para siempre.
/// Nunca se destruye ni se vuelve a instanciar, así el conteo de dientes
/// en el mapa permanece constante en cada ataque.
/// </summary>
public class FallingTeeth : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float fallSpeed = 10f;

    [Header("Reset")]
    [Tooltip("Tiempo que espera en el suelo antes de volver al techo")]
    [SerializeField] private float resetDelay = 2f;

    [Header("Damage")]
    [SerializeField] private int damageAmount = 10;

    // Posición inicial (techo), guardada al spawnear
    private Vector3 ceilingOrigin;

    private DealDamage dealDamage;

    [Networked] private bool isFalling { get; set; }
    [Networked] private bool hasHit { get; set; }

    public override void Spawned()
    {
        dealDamage = GetComponent<DealDamage>();
        if (dealDamage != null)
            dealDamage.SetAttacker(transform);

        // Guardamos la posición desde donde fue spawneado (= techo)
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
        if (other.CompareTag("Player"))
        {
            Debug.Log("[GroundSpike] Hit player");
            other.GetComponent<IDamageable>()?.TakeDamage(damageAmount, gameObject); // Aplica da�o al player
        }

    }

    /// <summary>
    /// Vuelve el diente a su posición original en el techo y lo deja
    /// inactivo. Queda esperando ahí hasta que BossAttackHandler llame
    /// StartFalling() de nuevo en un futuro ataque.
    /// </summary>
    void ResetToOrigin()
    {
        if (!Object.HasStateAuthority) return;

        transform.position = ceilingOrigin;
        hasHit = false;

        Debug.Log("[FallingTooth] Reseteado al techo, esperando próximo ataque");
    }

    /// <summary>
    /// Llamado por BossAttackHandler para activar este diente del pool
    /// y que empiece a caer. Si ya está cayendo o en cooldown post-impacto,
    /// no hace nada (evita reiniciar una caída en curso).
    /// </summary>
    public void StartFalling()
    {
        if (!Object.HasStateAuthority) return;
        if (isFalling) return;
        if (hasHit) return;

        transform.position = ceilingOrigin;
        hasHit = false;
        isFalling = true;

        Debug.Log("[FallingTooth] Comienza a caer");
    }
}