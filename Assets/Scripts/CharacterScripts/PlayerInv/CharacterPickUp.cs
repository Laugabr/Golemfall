using UnityEngine;
using Fusion;
public class CharacterPickUp : NetworkBehaviour
{
    [SerializeField] private float pickupDistance = 2.5f;

    public void TryPickUp()
    {
        if (!Object.HasInputAuthority) return;

        if (Physics.Raycast(transform.position, transform.forward, out var hit, pickupDistance))
        {
            var item = hit.collider.GetComponent<PickableItem>();
            if (item != null)
            {
                item.Rpc_Collect();
            }
        }
    }
}