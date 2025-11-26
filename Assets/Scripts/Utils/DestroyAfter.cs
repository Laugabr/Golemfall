using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DestroyAfter : MonoBehaviour
{
    [SerializeField] private float lifetime;

    private void Start()
    {
        StartCoroutine(DestroyAfterSeconds(lifetime));
    }

    IEnumerator DestroyAfterSeconds(float yieldlifetime)
    {
        yield return new WaitForSeconds(yieldlifetime);
        Destroy(gameObject);
    }
}
