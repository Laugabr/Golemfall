using UnityEngine;

public class BurnArea : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[BurnArea] Spawned");
        Destroy(gameObject, 2f);
    }
}
