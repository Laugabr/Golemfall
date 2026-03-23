using UnityEngine;
using Fusion;
public class CharacterPickUp : NetworkBehaviour
{
    [SerializeField] private float pickupRadius = 2.5f;
    
    public void TryPickUp()
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log(Object + " Try PickUp By server");

        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius);

        foreach (var hit in hits)
        {
            var item = hit.GetComponent<PickableItem>();
            if (item != null)
            {
                Debug.Log(item.name + " tries to be collected");
                item.Rpc_Collect(Object);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}