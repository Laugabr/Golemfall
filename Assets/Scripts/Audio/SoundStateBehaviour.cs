using UnityEngine;

/// <summary>
/// Reproduce un SoundEvent al entrar a un estado del Animator, con un retardo
/// configurable en segundos. Se agrega a un estado desde el Animator Controller
/// (Add Behaviour), no toca los clips ni el código networked.
///
/// Por qué esto funciona en multiplayer sin RPCs: los triggers de animación ya se
/// replican (NetCharacterAnimator / NetEnemyAnimator), así que TODOS los peers
/// entran al mismo estado y este behaviour corre en cada uno. El SoundEvent es 3D,
/// así que el AudioListener local lo posiciona y atenúa por distancia solo.
///
/// Dos modos:
///   - Single (repeat = false): suena UNA vez, a los "delaySeconds" de entrar al
///     estado. Para melee, rango, salto, dash, spells, take-damage, muerte.
///     delaySeconds = 0 => suena apenas entra (primer frame).
///   - Repeat (repeat = true): suena cada "repeatInterval" segundos mientras dure
///     el estado. Para la caminata (un paso cada X). El primer paso sale a los
///     "delaySeconds".
///
/// Mismo patrón que HealAtFrame / EnemyFireAtFrame, pero SIN filtrar por
/// StateAuthority: el sonido es cosmético y debe sonar en cada peer localmente.
/// </summary>
public class SoundStateBehaviour : StateMachineBehaviour
{
    [Tooltip("Sonido a reproducir. Arrastrá un SoundEvent (.asset).")]
    [SerializeField] private SoundEvent sound;

    [Tooltip("Segundos desde que entra al estado hasta que suena. 0 = inmediato.\n" +
             "Ajustalo a oído hasta que calce con el golpe / pisada.")]
    [SerializeField] private float delaySeconds = 0f;

    [Header("Repetición (para caminata)")]
    [Tooltip("Si está activo, el sonido se repite mientras dure el estado (pasos).")]
    [SerializeField] private bool repeat = false;

    [Tooltip("Segundos entre repeticiones cuando 'repeat' está activo.")]
    [SerializeField] private float repeatInterval = 0.4f;

    // Estado interno por reproducción. OJO: un StateMachineBehaviour se comparte
    // entre todas las instancias que usan ese Animator Controller, pero OnStateEnter
    // reinicia el timer en cada entrada, así que cada uso arranca limpio.
    private float _timer;
    private bool _done;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _timer = delaySeconds;
        _done = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_done && !repeat) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        sound?.Play(animator.transform.position);

        if (repeat)
            _timer += Mathf.Max(0.01f, repeatInterval);
        else
            _done = true;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Reset para que la próxima entrada al estado vuelva a sonar.
        _timer = delaySeconds;
        _done = false;
    }
}
