using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Representa una pista de música como asset (mismo razonamiento de GUID vs enum
/// que SoundEvent: renombrar/mover/reordenar no rompe referencias).
///
/// Soporta el patrón intro -> loop:
///   - introClip (opcional): se reproduce una sola vez al arrancar la pista.
///   - loopClip: se repite en loop indefinidamente.
/// Si no necesitás intro, dejá introClip en None y sólo se usa el loop.
///
/// Nota sobre loops sin costura: para un loop perfecto conviene que loopClip sea
/// un clip ya preparado para repetirse limpio. El handoff
/// intro -> loop por código puede dejar un micro-gap; si se necesita perfecto,
/// entonces usar un único loopClip seamless sin intro.
/// </summary>
[CreateAssetMenu(fileName = "MusicTrack_New", menuName = "Audio/Music Track")]
public class MusicTrack : ScriptableObject
{
    [Header("Clips")]
    [Tooltip("Opcional: se reproduce una vez al iniciar la pista, antes del loop.")]
    [SerializeField] private AudioClip introClip;

    [Tooltip("Clip principal que se repite en loop.")]
    [SerializeField] private AudioClip loopClip;

    [Header("Mezcla")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.7f;

    [Tooltip("Grupo del AudioMixer (normalmente Music).")]
    [SerializeField] private AudioMixerGroup mixerGroup;

    public AudioClip IntroClip => introClip;
    public AudioClip LoopClip => loopClip;
    public float Volume => volume;
    public AudioMixerGroup MixerGroup => mixerGroup;
    public bool HasIntro => introClip != null;
}
