using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakableObject : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private int hitsToBreak = 3;
    [SerializeField] private GameObject breakEffectPrefab;

    private int currentHits = 0;
    private bool isBroken = false;

    public void ReceiveHit()
    {
        if (isBroken) return;

        // Feedback visual de golpe
        GetComponent<HitFeedback>()?.FlashHit();

        currentHits++;

        if (currentHits >= hitsToBreak)
        {
            Break();
        }
        else
        {
            Debug.Log($"{gameObject.name} golpeado ({currentHits}/{hitsToBreak})");
        }
    }
    private void Break()
    {
        isBroken = true;

        if (breakEffectPrefab)
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);

        // Buscar automáticamente BreakableDrop en el mismo objeto
        BreakableDrop drop = GetComponent<BreakableDrop>();
        if (drop != null)
            drop.SpawnDrop(transform.position);

        Destroy(gameObject);
    }
}
