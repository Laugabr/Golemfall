using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private int damageAmount = 25;
    [SerializeField] private float lifetime = 3f;
    
    [SerializeField] private bool diesOnTouch;

    private Collider _collider;
    private GameObject owner;
    private float _speed;
    private Vector3 _direction;

    private Rigidbody _rb;

    [Range(0f, 1f)] // Sets the slider range from 0 to 100
    public float speedModifier; 
    [SerializeField] private GameObject HitImpact;

    void Start() => Destroy(gameObject, lifetime);

    public void Initialize(GameObject newOwner, int damageSent, float speed, Vector3 direction)
    {
        owner = newOwner;
        damageAmount = damageSent;
        _speed = speed;
        _direction = direction.normalized;
        
        Debug.Log(gameObject.name + " direction is " + _direction);
        
        _collider = GetComponent<Collider>();

        _rb = GetComponent<Rigidbody>();
        
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
        // Evitar dañar al dueño
        if (owner != null && other.gameObject == owner)
            return;

        // Punto de impacto más cercano
        Vector3 impactPoint = other.ClosestPoint(transform.position);

        // Si tiene vida → dañar y destruir / modificar
        if (other.TryGetComponent(out Health health))
        {
            health.TakeDamage(damageAmount);

            // Spawn de impacto en el punto exacto
            if (HitImpact != null)
                Instantiate(HitImpact, impactPoint, Quaternion.identity);

            if (!diesOnTouch)
            {
                _collider.enabled = false;
            }
            else
            {
                Destroy(gameObject);
            }

            _speed *= speedModifier;
            return;
        }

        // Impacto sin daño
        if (HitImpact != null)
            Instantiate(HitImpact, impactPoint, Quaternion.identity);

        if (!diesOnTouch)
        {
            _collider.enabled = false;
        }
        else
        {
            Destroy(gameObject);
        }

        _speed *= speedModifier;

        Debug.Log("Projectile collided with " + other.name);
    }
}
    