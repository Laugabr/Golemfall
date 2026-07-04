using Fusion;
using UnityEngine;

public class ParticleUnparenter : NetworkBehaviour
{
    [SerializeField] private Transform _visualParent; 

    private ParticleSystem _particleSystem;
    private bool _isParentDestroyed = false;

    public override void Spawned()
    {
        _visualParent = transform.parent;
        // Te desvinculás para sobrevivir en la jerarquía local
        transform.SetParent(null);

        // Se destruye este objeto suelto en 2 segundos
        Destroy(gameObject, 2f);

        if (TryGetComponent<ParticleSystem>(out var particleSystem))
        {
            _particleSystem = particleSystem;
        }
    }

    // Este método lo llama Fusion AUTOMÁTICAMENTE cuando el objeto de red original es despawneado
    public override void Despawned(NetworkRunner runner, bool hasStateAuthority)
    {
        if (!_isParentDestroyed)
        {
            _isParentDestroyed = true;
            if (_particleSystem != null)
            {
                _particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    public override void Render()
    {
        // Solo seguimos la posición si el objeto padre simulado sigue vivo en la escena
        if (_visualParent != null)
        {
            transform.position = _visualParent.position;
            transform.rotation = _visualParent.rotation;
        }
    }
}
