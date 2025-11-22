using Fusion;
using UnityEngine;

public class FollowParent : NetworkBehaviour
{
    public Transform Target;

    void LateUpdate()
    {
        if (Target == null) return;
        transform.position = Target.position;
        transform.rotation = Target.rotation;
    }
}