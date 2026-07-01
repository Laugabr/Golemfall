using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decide QUÉ música debe sonar (SRP: solo la decisión; reproducir es del
/// AudioManager). Vive uno por escena con música: le asignás su pista base en el
/// Inspector (menú -> música de menú; gameplay -> exploración).
///
/// Maneja una PILA de zonas: cuando el jugador entra a una MusicZone, esa pista
/// se apila y suena; al salir, se desapila y vuelve la que quedó debajo (o la
/// base si no queda ninguna). La pila evita quedar en silencio al salir de una
/// zona estando todavía dentro de otra solapada.
/// </summary>
public class MusicDirector : MonoBehaviour
{
    public static MusicDirector Instance { get; private set; }

    [Tooltip("Música por defecto de esta escena (menú o exploración).")]
    [SerializeField] private MusicTrack baseTrack;

    // Tope de la pila = lo que suena. Si está vacía, suena baseTrack.
    private readonly List<MusicTrack> _zoneStack = new List<MusicTrack>();

    private void Awake()
    {
        // NO es DontDestroyOnLoad: cada escena tiene el suyo con su propia baseTrack.
        Instance = this;
    }

    private void Start() => Apply();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Llamado por MusicZone cuando el jugador local entra a la zona.</summary>
    public void PushZone(MusicTrack track)
    {
        if (track == null) return;
        _zoneStack.Add(track);
        Apply();
    }

    /// <summary>Llamado por MusicZone cuando el jugador local sale de la zona.</summary>
    public void PopZone(MusicTrack track)
    {
        if (track == null) return;
        _zoneStack.Remove(track); // saca la primera coincidencia; alcanza acá
        Apply();
    }

    /// <summary>Pide al AudioManager la pista actual (tope de pila, o base si no hay zonas).</summary>
    private void Apply()
    {
        MusicTrack target = _zoneStack.Count > 0 ? _zoneStack[_zoneStack.Count - 1] : baseTrack;
        AudioManager.Instance?.PlayMusic(target);
    }
}
