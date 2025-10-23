using UnityEngine;

public class PlayerBreaker : MonoBehaviour
{
    [Header("Configuración de ataque")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackRadius = 0.7f;
    [SerializeField] private LayerMask breakableLayer;
    [SerializeField] private Transform attackOrigin; // un punto al frente del jugador 
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryBreakObject();
        }
    }

    private void TryBreakObject()
    {
        Collider[] hits = Physics.OverlapSphere(attackOrigin.position, attackRadius, breakableLayer);

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out BreakableObject breakable))
            {
                breakable.ReceiveHit();
                Debug.Log($"Golpeaste a {hit.name}");
                return; // solo golpea uno por clic
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackOrigin == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);
    }
}

