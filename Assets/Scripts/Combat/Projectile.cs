using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private int damageAmount = 25;
    [SerializeField] private float lifetime = 3f;

    private GameObject owner;
    private float _speed;
    private Vector3 _direction;

    void Start() => Destroy(gameObject, lifetime);

    public void Initialize(GameObject newOwner, int damageSent, float speed, Vector3 direction)
    {
        owner = newOwner;
        damageAmount = damageSent;
        _speed = speed;
        _direction = direction.normalized;

        // Rotar el proyectil para que mire hacia la dirección
        if (_direction != Vector3.zero)
            transform.forward = _direction;
    }

    void Update()
    {
        // Movimiento sobre el plano XZ
        transform.position += _direction * _speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Evita dañar al lanzador
        if (owner != null && other.gameObject == owner)
            return;

        if (other.TryGetComponent(out Health health))
        {
            health.TakeDamage(damageAmount);
            Destroy(gameObject);
        }
    }
}
    