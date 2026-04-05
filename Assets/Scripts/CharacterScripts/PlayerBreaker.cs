using UnityEngine;

public class PlayerBreaker : MonoBehaviour
{
    [Header("Configuración de ataque")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackRadius = 0.7f;
    [SerializeField] private LayerMask breakableLayer;
    [SerializeField] private Transform attackOrigin;
    
    private BreakableHitRequester hitRequester;

    private void Awake()
    {
        hitRequester = GetComponent<BreakableHitRequester>();
    }

    public void TryBreak()
    {
        if (hitRequester == null) return;
        hitRequester.RPC_RequestHit(
            attackOrigin.position,
            attackRadius,
            breakableLayer.value
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (attackOrigin == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);
    }
}