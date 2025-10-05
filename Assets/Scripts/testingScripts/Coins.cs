using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private int experienceAmount = 10;
    [SerializeField] private float rotationSpeed = 90f; // grados por segundo
    [SerializeField] private ParticleSystem pickupEffect;

    private void Update()
    {
        // Rotación visual simple
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificamos que sea el jugador
        if (other.CompareTag("Player"))
        {
            // Invocamos el evento de experiencia
            BasicEventsManager.OnExperienceGain?.Invoke(experienceAmount);
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
            // Destruimos la moneda completa
            Destroy(gameObject);
        }
    }
}