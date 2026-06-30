using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Servicio central de audio. Es el ÚNICO dueño de los AudioSource del juego (SRP):
/// los SoundEvent y MusicTrack sólo le piden que reproduzca, nunca tocan AudioSource.
///
/// Responsabilidades:
///   - Reproducir SFX 2D y 3D mediante un pool de AudioSource reutilizables.
///   - Reproducir música con crossfade entre pistas y soporte intro -> loop.
///   - Controlar el volumen de los buses del AudioMixer (Master / Music / SFX).
///
/// Vive como singleton DontDestroyOnLoad, igual que otros managers, para
/// sobrevivir el cambio de escena (menú -> gameplay) sin cortar la música.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [Tooltip("El GameAudioMixer. Necesario para controlar volúmenes por código.")]
    [SerializeField] private AudioMixer mixer;

    [Tooltip("Nombres EXACTOS de los parámetros expuestos en el Mixer (Fase 0).")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SfxVolume";

    [Header("SFX Pool")]
    [Tooltip("AudioSource para SFX simultáneos. 16 alcanza de sobra para este juego.")]
    [SerializeField] private int sfxPoolSize = 16;

    [Header("Música")]
    [Tooltip("Duración del crossfade entre pistas, en segundos.")]
    [SerializeField] private float musicFadeDuration = 1.5f;

    // Pool de sources para SFX. Se crean en runtime como hijos de este objeto.
    private AudioSource[] _sfxPool;
    private int _sfxIndex;

    // Dos sources para música: permiten hacer crossfade de una pista a otra.
    private AudioSource _musicA;
    private AudioSource _musicB;
    private AudioSource _activeMusic;   // el que está sonando ahora
    private MusicTrack _currentTrack;   // para no recargar la misma pista
    private Coroutine _musicRoutine;

    // Para el mute global: guardamos el volumen previo y lo restauramos al desmutear.
    private float _masterVolumeBeforeMute;
    private bool _isMuted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSfxPool();
        BuildMusicSources();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SETUP
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildSfxPool()
    {
        _sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var go = new GameObject($"SfxSource_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _sfxPool[i] = src;
        }
    }

    private void BuildMusicSources()
    {
        _musicA = CreateMusicSource("MusicSource_A");
        _musicB = CreateMusicSource("MusicSource_B");
        _activeMusic = _musicA;
    }

    private AudioSource CreateMusicSource(string sourceName)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f; // la música siempre es 2D
        return src;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SFX
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reproduce un SoundEvent en una posición del mundo. El spatialBlend (3D/2D)
    /// se toma del propio SoundEvent.
    /// </summary>
    public void PlaySfx(SoundEvent sound, Vector3 position)
    {
        if (sound == null) return;
        var clip = sound.GetClip();
        if (clip == null) return;

        var src = GetNextSfxSource();
        ConfigureSource(src, sound, clip);

        src.spatialBlend = sound.Is3D ? 1f : 0f;
        if (sound.Is3D)
        {
            src.transform.position = position;
            src.maxDistance = sound.MaxDistance;
            src.rolloffMode = AudioRolloffMode.Linear;
        }
        src.Play();
    }

    /// <summary>
    /// Reproduce un SoundEvent en 2D (volumen plano, sin posición). Para UI/botones.
    /// </summary>
    public void PlaySfx2D(SoundEvent sound)
    {
        if (sound == null) return;
        var clip = sound.GetClip();
        if (clip == null) return;

        var src = GetNextSfxSource();
        ConfigureSource(src, sound, clip);
        src.spatialBlend = 0f;
        src.Play();
    }

    private void ConfigureSource(AudioSource src, SoundEvent sound, AudioClip clip)
    {
        src.clip = clip;
        src.volume = sound.GetVolume();
        src.pitch = sound.GetPitch();
        src.loop = sound.Loop;
        src.outputAudioMixerGroup = sound.MixerGroup;
    }

    /// <summary>
    /// Devuelve un AudioSource libre del pool; si están todos ocupados, recicla el
    /// siguiente en orden (round-robin). Para SFX cortos es más que suficiente.
    /// </summary>
    private AudioSource GetNextSfxSource()
    {
        for (int i = 0; i < _sfxPool.Length; i++)
        {
            int idx = (_sfxIndex + i) % _sfxPool.Length;
            if (!_sfxPool[idx].isPlaying)
            {
                _sfxIndex = (idx + 1) % _sfxPool.Length;
                return _sfxPool[idx];
            }
        }
        var src = _sfxPool[_sfxIndex];
        _sfxIndex = (_sfxIndex + 1) % _sfxPool.Length;
        return src;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  MÚSICA
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reproduce una pista con crossfade desde la actual. Si ya está sonando esa
    /// misma pista, no hace nada (evita reinicios al re-entrar a una zona).
    /// </summary>
    public void PlayMusic(MusicTrack track)
    {
        if (track == null || track == _currentTrack) return;

        _currentTrack = track;
        if (_musicRoutine != null) StopCoroutine(_musicRoutine);
        _musicRoutine = StartCoroutine(CrossfadeTo(track));
    }

    /// <summary>Detiene la música con un fade out.</summary>
    public void StopMusic()
    {
        _currentTrack = null;
        if (_musicRoutine != null) StopCoroutine(_musicRoutine);
        _musicRoutine = StartCoroutine(FadeOutAndStop());
    }

    private IEnumerator CrossfadeTo(MusicTrack track)
    {
        // El source que sonaba se apaga; el otro arranca la pista nueva.
        AudioSource oldSource = _activeMusic;
        AudioSource newSource = (_activeMusic == _musicA) ? _musicB : _musicA;
        _activeMusic = newSource;

        newSource.outputAudioMixerGroup = track.MixerGroup;

        // Si hay intro, la tocamos una vez (sin loop); si no, vamos directo al loop.
        if (track.HasIntro)
        {
            newSource.clip = track.IntroClip;
            newSource.loop = false;
        }
        else
        {
            newSource.clip = track.LoopClip;
            newSource.loop = true;
        }

        newSource.volume = 0f;
        newSource.Play();

        // Crossfade. Usamos unscaled time para que la música no se frene al pausar.
        float target = track.Volume;
        float t = 0f;
        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / musicFadeDuration;
            newSource.volume = Mathf.Lerp(0f, target, k);
            oldSource.volume = Mathf.Lerp(oldSource.volume, 0f, k);
            yield return null;
        }
        newSource.volume = target;
        oldSource.Stop();

        // Handoff intro -> loop: esperamos lo que reste de la intro y empalmamos.
        if (track.HasIntro)
        {
            float remaining = track.IntroClip.length - musicFadeDuration;
            if (remaining > 0f) yield return new WaitForSecondsRealtime(remaining);

            newSource.clip = track.LoopClip;
            newSource.loop = true;
            newSource.volume = target;
            newSource.Play();
        }

        _musicRoutine = null;
    }

    private IEnumerator FadeOutAndStop()
    {
        AudioSource src = _activeMusic;
        float start = src.volume;
        float t = 0f;
        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, t / musicFadeDuration);
            yield return null;
        }
        src.Stop();
        _musicRoutine = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  VOLUMEN  (listo para cablear a un menú de opciones a futuro)
    // ─────────────────────────────────────────────────────────────────────────

    public void SetMasterVolume(float linear01) => SetMixerVolume(masterVolumeParam, linear01);
    public void SetMusicVolume(float linear01)  => SetMixerVolume(musicVolumeParam, linear01);
    public void SetSfxVolume(float linear01)    => SetMixerVolume(sfxVolumeParam, linear01);

    /// <summary>
    /// El AudioMixer trabaja en decibeles, no en lineal 0..1. Convertimos con la
    /// fórmula logarítmica estándar: 0 -> -80 dB (silencio), 1 -> 0 dB.
    /// </summary>
    private void SetMixerVolume(string param, float linear01)
    {
        if (mixer == null || string.IsNullOrEmpty(param)) return;
        float dB = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
        mixer.SetFloat(param, dB);
    }

    /// <summary>Mute/unmute global rápido sobre el bus Master.</summary>
    public void ToggleMute()
    {
        if (mixer == null) return;

        _isMuted = !_isMuted;
        if (_isMuted)
        {
            mixer.GetFloat(masterVolumeParam, out _masterVolumeBeforeMute);
            mixer.SetFloat(masterVolumeParam, -80f);
        }
        else
        {
            mixer.SetFloat(masterVolumeParam, _masterVolumeBeforeMute);
        }
    }
}
