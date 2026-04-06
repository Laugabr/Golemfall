using Fusion;
using UnityEngine;

public class BreakableHitRequester : NetworkBehaviour
{
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestHit(Vector3 position, float radius, int layerMask)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius, layerMask);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out BreakableObject breakable))
            {
                breakable.ReceiveHit();
                return;
            }
        }
    }
}
