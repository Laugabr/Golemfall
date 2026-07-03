using Fusion;
using UnityEngine;

/// <summary>
/// Decide qué música suena (SRP: solo decisión; reproducir es del AudioManager).
/// Vive uno por escena con su pista base asignada en el Inspector.
///
/// Enfoque por POLLING: cada 'checkInterval' segundos mira dónde está el jugador
/// local y elige la MusicZone de mayor prioridad que lo contiene (o la base si no
/// está en ninguna). Sin triggers, sin pila, sin parches: teleports, muertes y
/// respawns quedan bien solos, porque la música siempre sigue la posición real.
///
/// El chequeo es baratísimo (unas pocas zonas, 4 veces por segundo). Y como
/// AudioManager.PlayMusic ignora si ya suena esa pista, solo hay crossfade cuando
/// la zona realmente cambia.
/// </summary>
public class MusicDirector : MonoBehaviour
{
    public static MusicDirector Instance { get; private set; }

    [Tooltip("Música por defecto de la escena (menú o exploración).")]
    [SerializeField] private MusicTrack baseTrack;

    [Tooltip("Cada cuántos segundos se chequea la zona. 0.25 = 4 veces por segundo.")]
    [SerializeField] private float checkInterval = 0.25f;

    private Transform _localPlayer;
    private float _timer;

    private void Awake() => Instance = this;
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Start() => AudioManager.Instance?.PlayMusic(baseTrack);

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = checkInterval;

        AudioManager.Instance?.PlayMusic(ResolveTrack());
    }

    /// <summary>Zona de mayor prioridad que contiene al jugador local, o la base.</summary>
    private MusicTrack ResolveTrack()
    {
        var player = GetLocalPlayer();
        if (player == null) return baseTrack;

        MusicTrack best = baseTrack;
        int bestPriority = int.MinValue;

        foreach (var zone in MusicZone.Active)
        {
            if (zone.Track == null) continue;
            if (!zone.Contains(player.position)) continue;
            if (zone.Priority >= bestPriority)
            {
                bestPriority = zone.Priority;
                best = zone.Track;
            }
        }
        return best;
    }

    /// <summary>
    /// Busca y cachea el transform del jugador local (InputAuthority). La búsqueda
    /// solo corre hasta encontrarlo; después queda cacheado.
    /// </summary>
    private Transform GetLocalPlayer()
    {
        if (_localPlayer != null) return _localPlayer;

        foreach (var no in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (no.HasInputAuthority && no.CompareTag("Player"))
            {
                _localPlayer = no.transform;
                break;
            }
        }
        return _localPlayer;
    }
}