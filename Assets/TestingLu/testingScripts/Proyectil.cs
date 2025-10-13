using UnityEngine;

public class Proyectil : MonoBehaviour
{
    [SerializeField] private int damageAmount = 25;
    private GameObject owner; // referencia al que disparó

    [SerializeField] float lifetime;
        void Start() => Destroy(gameObject, lifetime);


    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
        Debug.Log("Owner is " + owner.name);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (owner != null && other.gameObject == owner)
            return;
        if (other.TryGetComponent<Health>(out Health health))
        {
            health.TakeDamage(damageAmount);
            Destroy(gameObject);
        }
    }
}
