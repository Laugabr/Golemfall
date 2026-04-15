using System.Text;
using Fusion;
using UnityEngine;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text statsText;                
    [SerializeField] private NetworkRunner runnerFallback;      

    private NetworkRunner runner;
    private PlayerStats localPlayerStats;
    private bool subscribed = false;

    private void Start()
    {
        TryFindLocalPlayerStats();
    }

    private void TryFindLocalPlayerStats()
    {
        // Si no hay runner, intento volver a buscar
        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        if (runner == null)
        {
            Debug.LogWarning("[PlayerStatsUI] No se encontró NetworkRunner en la escena.");
            return;
        }

        // Busco todos los PlayerStats en escena y elijo aquel cuyo NetworkObject coincida con el LocalPlayer
        var all = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        foreach (var ps in all)
        {
            // algunos objetos pueden no tener el NetworkObject inicializado aún
            if (ps == null || ps.Object == null) continue;
            if (!ps.Object.IsValid) continue;

            if (ps.Object.InputAuthority == runner.LocalPlayer)
            {
                BindToPlayerStats(ps);
                return;
            }
        }

        // si no lo encontré, lo intentaré de nuevo dentro de un corto delay (por si se spawnea después)
        Invoke(nameof(TryFindLocalPlayerStats), 0.5f);
    }

    private void BindToPlayerStats(PlayerStats ps)
    {
        if (ps == null) return;
        if (subscribed && localPlayerStats == ps) return;

        // Si ya había uno suscripto, removemos la suscripción
        if (subscribed && localPlayerStats != null)
            localPlayerStats.OnStatsChanged.RemoveListener(UpdateStatsText);

        localPlayerStats = ps;
        localPlayerStats.OnStatsChanged.AddListener(UpdateStatsText);
        subscribed = true;

        // Actualizo la UI inmediatamente con los valores actuales en localStats
        UpdateStatsText();
    }

    public void UpdateStatsText()
    {
        if (statsText == null)
        {
            Debug.LogWarning("[PlayerStatsUI] statsText no asignado.");
            return;
        }

        if (localPlayerStats == null)
        {
            statsText.text = "Stats: -";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== Player Stats ===");

        foreach (var si in localPlayerStats.localStats)
        {
            sb.AppendLine($"{si.statType}: {si.statValue}");
        }

        statsText.text = sb.ToString();
    }

    private void OnDestroy()
    {
        if (subscribed && localPlayerStats != null)
        {
            localPlayerStats.OnStatsChanged.RemoveListener(UpdateStatsText);
        }
    }
}

