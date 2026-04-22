using UnityEngine;
using Fusion;

public class FallingTeeth : NetworkBehaviour
{
    [SerializeField] private float fallSpeed = 10f;

    private DealDamage dealDamage;
    private bool hasHit = false;

    public override void Spawned()
    {
        dealDamage = GetComponent<DealDamage>();

        if (dealDamage != null)
        {
            dealDamage.SetAttacker(transform);
        }
    }

    private void Update()
    {
        if (!Object.HasStateAuthority) return;

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (hasHit) return;

        if (other.CompareTag("Player") || other.CompareTag("Ground"))
        {
            hasHit = true;

            Debug.Log("[FallingTooth] Impact");

            if (other.CompareTag("Player"))
            {
                dealDamage?.ApplyDamage(other.gameObject);
            }

            Runner.Despawn(Object);
        }
    }
}