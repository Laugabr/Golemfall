using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Representa UN sonido del juego como un asset (ScriptableObject).
///
/// Por qué un asset y no un enum:
///   Unity serializa los enums por su valor numérico. Si se reordena o borra
///   un valor, todas las referencias serializadas quedan apuntando al número
///   viejo y se corrompen en silencio (el problema de GameEventType).
///   Las referencias a assets, en cambio, se serializan por GUID: podés
///   renombrar, mover o reordenar sin romper nada, y si se borra el asset el campo
///   queda visiblemente en None en vez de apuntar al sonido equivocado.
///
/// Cada componente que necesite sonar referencia el SoundEvent en el Inspector
/// (arrastrándolo) y llama a Play()/Play2D(). El SoundEvent NO reproduce nada por
/// sí mismo: delega en el AudioManager, único dueño de los AudioSource (SRP).
/// </summary>
[CreateAssetMenu(fileName = "Sfx_New", menuName = "Audio/Sound Event")]
public class SoundEvent : ScriptableObject
{
    [Header("Clips")]
    [Tooltip("Uno o varios clips. Si hay más de uno, se elige al azar en cada Play " +
             "(ideal para variantes como step grass 01/02/03).")]
    [SerializeField] private AudioClip[] clips;

    [Header("Volumen y pitch")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Tooltip("Se elige un pitch al azar entre min y max en cada Play, para que los " +
             "sonidos repetidos no suenen idénticos. Dejá ambos en 1 para pitch fijo.")]
    [SerializeField] private float pitchMin = 1f;
    [SerializeField] private float pitchMax = 1f;

    [Header("Reproducción")]
    [Tooltip("Si el sonido se repite en loop. Normalmente false para SFX.")]
    [SerializeField] private bool loop = false;

    [Tooltip("3D = posicionado en el mundo, baja de volumen con la distancia al " +
             "jugador (ataques, pasos). 2D = volumen plano, ignora la posición (UI).")]
    [SerializeField] private bool is3D = true;

    [Tooltip("Sólo 3D: distancia a la que el sonido deja de oírse.")]
    [SerializeField] private float maxDistance = 25f;

    [Header("Mezcla")]
    [Tooltip("Grupo del AudioMixer por el que sale (SFX o UI). Controla el bus.")]
    [SerializeField] private AudioMixerGroup mixerGroup;

    // Getters de sólo lectura que usa el AudioManager para configurar el AudioSource.
    public bool Loop => loop;
    public bool Is3D => is3D;
    public float MaxDistance => maxDistance;
    public AudioMixerGroup MixerGroup => mixerGroup;
    public float GetVolume() => volume;

    /// <summary>Pitch al azar dentro del rango configurado.</summary>
    public float GetPitch() => Random.Range(pitchMin, pitchMax);

    /// <summary>Devuelve un clip al azar de la lista (o el único, si hay uno solo).</summary>
    public AudioClip GetClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips.Length == 1 ? clips[0] : clips[Random.Range(0, clips.Length)];
    }

    /// <summary>
    /// Reproduce el sonido en una posición del mundo. El spatialBlend (3D/2D) se
    /// toma del propio asset. Usar para ataques, pasos, dash, explosiones, etc.
    /// </summary>
    public void Play(Vector3 position)
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[SoundEvent] No hay AudioManager en escena al reproducir '{name}'.");
            return;
        }
        AudioManager.Instance.PlaySfx(this, position);
    }

    /// <summary>
    /// Reproduce el sonido en 2D (volumen plano, sin posición). Para UI/botones.
    /// </summary>
    public void Play2D()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[SoundEvent] No hay AudioManager en escena al reproducir '{name}'.");
            return;
        }
        AudioManager.Instance.PlaySfx2D(this);
    }
}
