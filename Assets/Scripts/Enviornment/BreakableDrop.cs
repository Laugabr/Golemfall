using Fusion;
using UnityEngine;

[System.Serializable]
public class BreakableDrop : NetworkBehaviour
{
    [Header("Drop de ítem")]
    [SerializeField] private NetworkObject itemPrefab;

    public void SpawnDrop(Vector3 position)
    {
        Debug.Log($"SpawnDrop llamado — HasStateAuthority:{Object.HasStateAuthority} prefab:{itemPrefab}");
        if (!Object.HasStateAuthority) return;
        if (itemPrefab == null) return;

        Runner.Spawn(
            itemPrefab,
            position + Vector3.up * 0.5f,
            Quaternion.identity
        );
    }
}

