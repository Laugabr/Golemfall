using Fusion;
using UnityEngine;

public class ParticleUnparenter : NetworkBehaviour
{
    private Transform _visualParent; 
    private bool _isParentDestroyed = false;

    public override void Spawned()
    {
        _visualParent = transform.parent;
        // El Nieto se libera en la escena para no morir con el Despawn
        transform.SetParent(null);

        Destroy(gameObject, 5f);

    }

    public override void Render()
    {
        transform.position = _visualParent.position;
        transform.rotation = _visualParent.rotation;
        }
}
