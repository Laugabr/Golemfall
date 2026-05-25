using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"SpawnPoint: {other.name} entered spawn point trigger.");
        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
            health.SetLastSpawnPoint(transform.position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}