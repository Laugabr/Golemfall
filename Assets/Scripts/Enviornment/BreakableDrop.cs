using UnityEngine;

[System.Serializable]
public class BreakableDrop : MonoBehaviour
{
    [Header("Drop de ítem")]
    [SerializeField] private GameObject itemPrefab; // Prefab del ItemPickup

    public void SpawnDrop(Vector3 position)
    {
        if (itemPrefab)
            Instantiate(itemPrefab, position + Vector3.up * 0.2f, Quaternion.identity);
    }
}

