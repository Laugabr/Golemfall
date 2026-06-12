using UnityEngine;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// Trigger alrededor del weak point del boss.
/// Si un player permanece dentro durante 'timeToTrigger' segundos,
/// dispara una oleada de picos de suelo.
///
/// Lógica:
///   - Al entrar un player, inicia un countdown.
///   - Si el player sale antes de que termine, cancela.
///   - Si llega a cero con al menos un player adentro, dispara picos.
///   - Cooldown post-disparo para no spamear.
///
/// Setup en escena:
///   - Collider trigger alrededor del weak point.
///   - Referencia a BossAI en el inspector.
/// </summary>
public class BossProximityTrigger : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI bossAI;

    [Header("Settings")]
    [Tooltip("Segundos que el player debe estar adentro antes de disparar picos")]
    [SerializeField] private float timeToTrigger = 3f;
    [Tooltip("Cooldown después de disparar (evita spam)")]
    [SerializeField] private float cooldown = 6f;

    private HashSet<Transform> playersInside = new();
    private bool isOnCooldown = false;
    private float playerEnteredTime = -1f;
    private bool countdownActive = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (!other.CompareTag("Player")) return;

        Transform root = other.transform.root;
        if (playersInside.Contains(root)) return;

        playersInside.Add(root);
        Debug.Log($"[Proximity] Player entró al weak point: {root.name}");

        // Inicia el countdown si no estaba corriendo
        if (!countdownActive && !isOnCooldown)
        {
            countdownActive = true;
            playerEnteredTime = Time.time;
            Debug.Log($"[Proximity] Countdown iniciado: {timeToTrigger}s");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Transform root = other.transform.root;
        playersInside.Remove(root);

        Debug.Log($"[Proximity] Player salió: {root.name} — {playersInside.Count} restantes");

        // Si no queda nadie, cancela el countdown
        if (playersInside.Count == 0)
        {
            countdownActive = false;
            playerEnteredTime = -1f;
            Debug.Log("[Proximity] Countdown cancelado — arena vacía");
        }
    }

    private void Update()
    {
        if (!Object.HasStateAuthority) return;
        if (!countdownActive) return;
        if (isOnCooldown) return;
        if (playersInside.Count == 0) return;

        if (Time.time >= playerEnteredTime + timeToTrigger)
        {
            FireSpikes();
        }
    }

    void FireSpikes()
    {
        countdownActive = false;
        isOnCooldown = true;
        playerEnteredTime = -1f;

        Debug.Log("[Proximity] ¡Tiempo! → disparando Ground Spikes");
        bossAI.TriggerGroundSpikes();

        Invoke(nameof(ResetCooldown), cooldown);
    }

    void ResetCooldown()
    {
        isOnCooldown = false;

        // Si hay players adentro todavía, reinicia el countdown
        if (playersInside.Count > 0)
        {
            countdownActive = true;
            playerEnteredTime = Time.time;
            Debug.Log("[Proximity] Cooldown terminado — reiniciando countdown");
        }
    }
}