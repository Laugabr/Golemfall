using Fusion;
using UnityEngine;

[System.Serializable]
public class BreakableDrop : NetworkBehaviour
{
    [Header("Drop de ítem")]
    [SerializeField] private NetworkObject itemPrefab;

    public void SpawnDrop(Vector3 position)
    {
        Debug.Log($"SpawnDrop — HasStateAuthority:{Object.HasStateAuthority} Runner null:{Runner == null} prefab:{itemPrefab?.name}");
        if (!Object.HasStateAuthority) return;
        if (itemPrefab == null) return;
        Runner.Spawn(itemPrefab, position + Vector3.up * 0.5f, Quaternion.identity);
    }
}

