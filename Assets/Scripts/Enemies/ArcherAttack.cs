/*using UnityEngine;

public class ArcherAttack : MonoBehaviour
{
    public Transform firePoint; // posición desde donde sale la flecha
    public GameObject arrowPrefab;

    public float arrowSpeed = 10f;
    public int arrowDamage = 1;
    public float attackCooldown = 0.5f;

    private float cooldownTimer;

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (Input.GetButtonDown("Fire1") && cooldownTimer <= 0)
        {
            Shoot();
            cooldownTimer = attackCooldown;
        }
    }

    void Shoot()
    {
        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

        // Dirección según hacia donde esté mirando
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        arrow.GetComponent<ArrowProjectile>().Initialize(dir, arrowSpeed, arrowDamage);

        // Rotación opcional de la flecha
        arrow.transform.right = dir;
    }
}*/
