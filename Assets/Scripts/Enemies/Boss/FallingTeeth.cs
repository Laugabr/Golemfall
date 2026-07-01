using UnityEngine;
using Fusion;

public class FallingTeeth : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float fallSpeed = 10f;

    [Header("Reset")]
    [Tooltip("Tiempo que espera antes de volver al techo")]
    [SerializeField] private float resetDelay = 2f;

    [Header("Damage")]
    [SerializeField] private int damageAmount = 10;

    private Vector3 ceilingOrigin;

    [Networked] private bool isFalling { get; set; }
    [Networked] private bool hasHit { get; set; }

    public override void Spawned()
    {
        ceilingOrigin = transform.position;

        if (Object.HasStateAuthority)
        {
            isFalling = false;
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

        if (!hitPlayer && !hitGround) return;

        // Detener la caída
        hasHit = true;
        isFalling = false;

        if (hitPlayer)
        {
            Debug.Log("[FallingTooth] Hit player");
            other.GetComponent<IDamageable>()?.TakeDamage(damageAmount, gameObject);
        }
        else
        {
            Debug.Log("[FallingTooth] Hit suelo");
        }

        // Programar retorno al techo
        Invoke(nameof(ResetToOrigin), resetDelay);
    }

    void ResetToOrigin()
    {
        if (!Object.HasStateAuthority) return;

        transform.position = ceilingOrigin;
        hasHit = false;

        Debug.Log("[FallingTooth] Reseteado al techo");
    }

    public void StartFalling()
    {
        if (!Object.HasStateAuthority) return;
        if (isFalling) return;
        if (hasHit) return;

        transform.position = ceilingOrigin;
        isFalling = true;

        Debug.Log("[FallingTooth] Comienza a caer");
    }
}