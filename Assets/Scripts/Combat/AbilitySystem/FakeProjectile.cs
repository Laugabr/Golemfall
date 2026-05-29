// FakeProjectile.cs
using UnityEngine;

public class FakeProjectile : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private uint _ownerId;

    public void Initialize(uint ownerId, Vector3 direction, float speed, float lifetime)
    {
        _ownerId  = ownerId;
        _direction = direction.normalized;
        _speed     = speed;

        FakeProjectileRegistry.Register(ownerId, this);
        Destroy(gameObject, lifetime); // expiró solo si el real tarda demasiado
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;
    }

    private void OnDestroy()
    {
        // Nada que limpiar: el Registry skipea nulls automáticamente en Dequeue
    }
}