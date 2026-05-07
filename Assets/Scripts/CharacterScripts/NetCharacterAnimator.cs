using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

/// <summary>
/// Maneja el estado de animación del personaje networked con CLIENT-SIDE PREDICTION.
///
/// Modelo de prediction de Fusion 2:
///   - Tanto el StateAuthority (host) como el InputAuthority (cliente local) corren
///     FixedUpdateNetwork() y tienen acceso al input vía GetInput().
///   - El cliente con InputAuthority predice la animación INMEDIATAMENTE, sin esperar
///     la confirmación del server. Esto hace que el jugador local sienta sus inputs
///     responsivos (sin los ~100ms de delay de ida + vuelta al server).
///   - Cuando llega el snapshot autoritativo y Fusion resimula los ticks predichos,
///     si la predicción coincide con el estado autoritativo no pasa nada.
///   - Si difiere (ej: el server rechazó la acción por cooldown), Fusion sobrescribe
///     las [Networked] y el Animator se autocorrige con un blend natural.
///   - Los proxies (otros jugadores) leen las [Networked] en Render() — no predicen.
///
/// Reglas clave de la doc oficial de Fusion 2 sobre animaciones:
///   1. Aplicar cambios al Animator solo cuando GetInput() devuelve true.
///   2. Solo en ticks Forward (Runner.IsForward) — NUNCA durante resimulación.
///      Si se dispara un trigger durante una resimulación, se llamaría varias veces
///      por frame (Fusion resimula múltiples ticks por render frame).
///   3. La animación se autocorrige sola si la predicción falla — no hay que
///      cancelarla manualmente.
///
/// Animator parameters expected:
///   bool    isWalking
///   bool    isDashing
///   bool    isGrounded
///   int     idleType
///   trigger jumpTrigger
///   trigger meleeTrigger
///   trigger rangeTrigger
/// </summary>
public class NetCharacterAnimator : NetworkBehaviour
{
    [SerializeField] private SimpleKCC kcc;
    [SerializeField] private Animator animator;
    [SerializeField] private NetCharacterController controller;

    [Header("Idle Settings")]
    [SerializeField] private float chanceToChange = 0.15f;
    [SerializeField] private float variantDuration = 4f;

    // Networked state — el StateAuthority es la fuente de verdad. El cliente con
    // InputAuthority puede escribir estas propiedades como predicción local; Fusion
    // las sobrescribe automáticamente cuando llega el snapshot autoritativo.

    [Networked] private NetworkBool NetIsWalking { get; set; }
    [Networked] private NetworkBool NetIsGrounded { get; set; }
    [Networked] private int NetIdleType { get; set; }

    // Triggers replicados via "tick stamp": cuando el stamp cambia, se dispara una vez.
    [Networked] private int NetJumpTick { get; set; }
    [Networked] private int NetMeleeTick { get; set; }
    [Networked] private int NetRangeTick { get; set; }

    // Último tick observado localmente — usado para detectar cambios en Render().
    private int _lastJumpTick;
    private int _lastMeleeTick;
    private int _lastRangeTick;

    // Cached parameter hashes
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsDashing = Animator.StringToHash("isDashing");
    private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int IdleTypeHash = Animator.StringToHash("idleType");
    private static readonly int JumpTrigger = Animator.StringToHash("jumpTrigger");
    private static readonly int MeleeTrigger = Animator.StringToHash("meleeTrigger");
    private static readonly int RangeTrigger = Animator.StringToHash("rangeTrigger");

    // Estado para detectar "edge" de presionado en FUN. Se mantiene por peer
    // (host e input authority cada uno lleva el suyo) para que la predicción
    // funcione independientemente.
    private NetworkButtons _previousButtons;

    // Estado del idle randomizer — SOLO se usa en el StateAuthority (es no-determinístico).
    private float _serverIdleTimer;
    private bool _serverIsInVariant;

    // Para detectar el inicio del dash y resetear el idle.
    private bool _wasDashingLastTick;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (kcc == null)
            kcc = GetComponent<SimpleKCC>();

        if (controller == null)
            controller = GetComponent<NetCharacterController>();
    }

    public override void Spawned()
    {
        // Inicializar last-seen ticks para que un proxy que entra tarde a la sesión
        // no dispare triggers viejos cuando recibe el primer snapshot.
        _lastJumpTick = NetJumpTick;
        _lastMeleeTick = NetMeleeTick;
        _lastRangeTick = NetRangeTick;
    }

    /// <summary>
    /// FixedUpdateNetwork corre en TODOS los peers que tienen input disponible:
    /// host (StateAuthority) y cliente local (InputAuthority). Los proxies devuelven
    /// false en GetInput() y se filtran abajo, por lo que NO predicen.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (animator == null) return;

        // Si no hay input disponible, este peer es un proxy o el input se perdió
        // este tick. En ambos casos no predecimos ni escribimos nada.
        if (!GetInput(out NetInputPlayer input))
        {
            return;
        }

        // CRÍTICO: solo aplicar cambios al Animator durante ticks Forward.
        // Durante una resimulación (Runner.IsResimulation == true), Fusion vuelve
        // a llamar FixedUpdateNetwork() para reaplicar los inputs ya guardados;
        // si disparamos triggers ahí, se reproducirían varias veces por frame.
        // La doc de Fusion 2 es categórica con esta regla.
        if (!Runner.IsForward)
        {
            // Aún así actualizamos _previousButtons para que el WasPressed del
            // próximo tick Forward esté bien sincronizado con el flujo de inputs.
            _previousButtons = input.Buttons;
            return;
        }

        // A partir de acá: estamos en un tick Forward y tenemos input válido.
        // Esto corre tanto en el host como en el cliente con InputAuthority.

        // Detectar inicio de dash (controller es la fuente de verdad de IsDashing).
        bool dashing = controller != null && controller.IsDashing;
        bool dashJustStarted = dashing && !_wasDashingLastTick;
        if (dashJustStarted) ResetIdleLocal();
        _wasDashingLastTick = dashing;

        // ── TRIGGERS PREDICHOS 
        // El cliente local llama animator.SetTrigger() inmediatamente, y además
        // actualiza _lastXxxTick para que Render() no lo redispare cuando llegue
        // el snapshot autoritativo del server. El host hace exactamente lo mismo.
        // Si la predicción falla (server rechaza la acción), el Animator hace blend
        // natural al estado correcto cuando llegue el snapshot.

        // JUMP: solo si está en piso (el cliente puede chequear esto localmente).
        bool jumpPressed = input.Buttons.WasPressed(_previousButtons, InputButton.Jump);
        if (jumpPressed && kcc.IsGrounded)
        {
            // El StateAuthority escribe la [Networked] real. El InputAuthority
            // también la escribe como predicción local; Fusion la sobrescribe
            // automáticamente con el valor autoritativo cuando llega el snapshot.
            NetJumpTick = Runner.Tick;

            // Dispara el trigger localmente AHORA, sin esperar al snapshot.
            animator.SetTrigger(JumpTrigger);
            _lastJumpTick = NetJumpTick; // Evita que Render() lo redispare.

            ResetIdleLocal();
        }

        // MELEE
        if (input.Buttons.WasPressed(_previousButtons, InputButton.BasicAttack))
        {
            NetMeleeTick = Runner.Tick;
            animator.SetTrigger(MeleeTrigger);
            _lastMeleeTick = NetMeleeTick;
            ResetIdleLocal();
        }

        // RANGE (FirstSkill)
        if (input.Buttons.WasPressed(_previousButtons, InputButton.FirstSkill))
        {
            NetRangeTick = Runner.Tick;
            animator.SetTrigger(RangeTrigger);
            _lastRangeTick = NetRangeTick;
            ResetIdleLocal();
        }

        // ── BOOLEANOS PREDICHOS 
        // isWalking e isGrounded los escribimos como [Networked]. Tanto host como
        // InputAuthority escriben — en el cliente queda como predicción local.
        UpdateMovementFlags(input.Direction);

        // ── IDLE RANDOMIZER (NO predicho) 
        // Usa Random.value, que es no-determinístico — si lo corriéramos en el
        // InputAuthority, el cliente y el server elegirían variantes distintas.
        // Por eso queda restringido al StateAuthority.
        if (HasStateAuthority)
        {
            UpdateIdleRandomizerServer();
        }

        _previousButtons = input.Buttons;
    }

    private void UpdateMovementFlags(Vector3 inputDir)
    {
        bool dashing = controller != null && controller.IsDashing;
        bool hasDir = inputDir.magnitude > 0.1f;
        NetIsWalking = hasDir && !dashing;
        NetIsGrounded = kcc.IsGrounded;
    }

    private void UpdateIdleRandomizerServer()
    {
        bool dashing = controller != null && controller.IsDashing;

        // Cancelar variantes si se mueve / está en aire / está dasheando
        if (!kcc.IsGrounded || kcc.RealVelocity.sqrMagnitude > 0.5f || dashing)
        {
            ResetIdleServer();
            return;
        }

        // Cancelar si actualmente está en un estado con tag "Attack"
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsTag("Attack"))
        {
            ResetIdleServer();
            return;
        }

        _serverIdleTimer -= Runner.DeltaTime;

        if (_serverIdleTimer <= 0f)
        {
            if (!_serverIsInVariant)
            {
                if (Random.value < chanceToChange)
                {
                    int choice = Random.Range(1, 3); // 1 o 2
                    NetIdleType = choice;
                    _serverIdleTimer = variantDuration;
                    _serverIsInVariant = true;
                }
                else
                {
                    _serverIdleTimer = 2f;
                }
            }
            else
            {
                NetIdleType = 0;
                _serverIdleTimer = 5f;
                _serverIsInVariant = false;
            }
        }
    }

    /// <summary>
    /// Reset del idle ejecutado en el StateAuthority. Modifica la [Networked].
    /// </summary>
    private void ResetIdleServer()
    {
        if (NetIdleType != 0)
            NetIdleType = 0;

        _serverIsInVariant = false;
        _serverIdleTimer = 2f;
    }

    /// <summary>
    /// Reset del idle que pueden llamar tanto host como InputAuthority. En el
    /// cliente, escribir NetIdleType es una predicción local que será sobrescrita
    /// por el snapshot autoritativo del host.
    /// </summary>
    private void ResetIdleLocal()
    {
        if (HasStateAuthority)
        {
            ResetIdleServer();
        }
        else if (NetIdleType != 0)
        {
            // Predicción local: el cliente "adivina" que el host también va a
            // resetear el idle (porque acaba de empezar una acción). Fusion lo
            // corrige si difiere.
            NetIdleType = 0;
        }
    }

    /// <summary>
    /// Render corre en TODOS los peers a framerate de pantalla. Acá leemos las
    /// [Networked] y las aplicamos al Animator. Para el cliente con InputAuthority,
    /// el trigger ya fue disparado en FixedUpdateNetwork() (predicción), por lo
    /// que _lastXxxTick == NetXxxTick y NO se redispara.
    /// Para los proxies, esta es la única vía por la que se enteran de los triggers.
    /// </summary>
    public override void Render()
    {
        if (animator == null) return;

        // IsDashing viene del controller (replicado, fuente única de verdad).
        bool dashing = controller != null && controller.IsDashing;

        // Booleanos
        animator.SetBool(IsWalking, NetIsWalking);
        animator.SetBool(IsDashing, dashing);
        animator.SetBool(IsGrounded, NetIsGrounded);
        animator.SetInteger(IdleTypeHash, NetIdleType);

        // Triggers via tick-stamp diff. En el cliente con InputAuthority, los
        // _lastXxxTick ya fueron actualizados al predecir, así que estos ifs
        // dan false y no se redispara. En proxies y en el host, funcionan
        // como antes para mostrar la animación.
        if (NetJumpTick != _lastJumpTick)
        {
            _lastJumpTick = NetJumpTick;
            animator.SetTrigger(JumpTrigger);
        }

        if (NetMeleeTick != _lastMeleeTick)
        {
            _lastMeleeTick = NetMeleeTick;
            animator.SetTrigger(MeleeTrigger);
        }

        if (NetRangeTick != _lastRangeTick)
        {
            _lastRangeTick = NetRangeTick;
            animator.SetTrigger(RangeTrigger);
        }
    }

    // Llamado por el Animation Event en ani_player_jumpStart.
    // Actualmente no-op, se mantiene para silenciar el warning "has no receiver".
    private void FinalizeJump() { }
}