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
///   - Los proxies (otros jugadores) leen las [Networked] en Render() — no predicen.
///
/// SEPARACIÓN OWNER vs PROXY (clave para evitar doble disparo):
///   - El owner (HasInputAuthority || HasStateAuthority) dispara triggers desde
///     FixedUpdateNetwork() detectando WasPressed sobre el input. Nunca los lee
///     desde la [Networked] en Render() — esa variable solo existe para
///     transmitir al resto.
///   - Los proxies (!HasInputAuthority && !HasStateAuthority) hacen lo opuesto:
///     ignoran el input y solo reaccionan a cambios de las [Networked] en Render().
///   - Resultado: cada peer dispara cada trigger exactamente UNA vez.
///
/// Reglas clave de la doc oficial de Fusion 2 sobre animaciones:
///   1. Aplicar cambios al Animator solo cuando GetInput() devuelve true.
///   2. Solo en ticks Forward (Runner.IsForward) — NUNCA durante resimulación.
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

    // Networked state — leído por proxies en Render(). El owner las escribe en FUN
    // pero NO las lee en Render() (usa su propio estado predicho directamente).

    [Networked] private NetworkBool NetIsWalking { get; set; }
    [Networked] private NetworkBool NetIsGrounded { get; set; }
    [Networked] private NetworkBool NetIsFalling { get; set; }
    [Networked] private int NetIdleType { get; set; }

    // Triggers replicados para los PROXIES via "tick stamp": cuando el stamp
    // cambia, el proxy dispara el trigger una vez. El owner NO los lee.
    [Networked] private int NetJumpTick { get; set; }
    [Networked] private int NetMeleeTick { get; set; }
    [Networked] private int NetRangeTick { get; set; }

    // Último tick observado por los PROXIES — usado para detectar cambios en Render().
    // En el owner, estas variables nunca se actualizan (porque no dispara desde Render).
    private int _lastJumpTick;
    private int _lastMeleeTick;
    private int _lastRangeTick;

    // Cached parameter hashes
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsDashing = Animator.StringToHash("isDashing");
    private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int IsFallingHash = Animator.StringToHash("isFalling");
    private static readonly int IdleTypeHash = Animator.StringToHash("idleType");
    private static readonly int JumpTriggerHash = Animator.StringToHash("jumpTrigger");
    private static readonly int MeleeTriggerHash = Animator.StringToHash("meleeTrigger");
    private static readonly int RangeTriggerHash = Animator.StringToHash("rangeTrigger");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("verticalVelocity");

    // Estado para detectar "edge" de presionado en FUN. Se mantiene por peer.
    private NetworkButtons _previousButtons;

    // Estado del idle randomizer — SOLO se usa en el StateAuthority (es no-determinístico).
    private float _serverIdleTimer;
    private bool _serverIsInVariant;

    // Para detectar el inicio del dash y resetear el idle.
    private bool _wasDashingLastTick;

    // Umbral: por debajo de este valor de velocidad vertical = está cayendo.
    // Ajustalo si ves falsos positivos (ej: pequeñas rampas).
    private const float FallVelocityThreshold = -1.5f;
    // Cache: ¿soy el owner de este personaje? (host viendo su personaje, o cliente local).
    // Si es false, soy un proxy y solo reacciono a [Networked] en Render().
    private bool IsOwner => HasInputAuthority || HasStateAuthority;

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
    /// FixedUpdateNetwork corre en host (StateAuthority) y cliente local (InputAuthority).
    /// Los proxies devuelven false en GetInput() y se filtran abajo.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (animator == null) return;

        if (!GetInput(out NetInputPlayer input))
        {
            return;
        }

        // CRÍTICO: solo aplicar cambios al Animator durante ticks Forward.
        // Durante una resimulación, Fusion vuelve a llamar FixedUpdateNetwork()
        // para reaplicar los inputs ya guardados; si disparamos triggers ahí,
        // se reproducirían varias veces por frame. La doc de Fusion 2 lo prohíbe
        // explícitamente.
        if (!Runner.IsForward)
        {
            // Aún así actualizamos _previousButtons para que el WasPressed del
            // próximo tick Forward esté bien sincronizado con el flujo de inputs.
            _previousButtons = input.Buttons;
            return;
        }

        // Detectar inicio de dash (controller es la fuente de verdad de IsDashing).
        bool dashing = controller != null && controller.IsDashing;
        bool dashJustStarted = dashing && !_wasDashingLastTick;
        if (dashJustStarted) ResetIdleLocal();
        _wasDashingLastTick = dashing;

        // ── TRIGGERS PREDICHOS (camino del OWNER) ───────────────────────────
        // El owner (host o input authority) dispara animator.SetTrigger() acá
        // basándose en el INPUT. Además escribe la [Networked] para los proxies.
        // El owner NUNCA dispara triggers desde Render() — eso es solo para
        // proxies. Esta separación garantiza un disparo único.

        // JUMP: solo si está en piso.
        bool jumpPressed = input.Buttons.WasPressed(_previousButtons, InputButton.Jump);
        if (jumpPressed && kcc.IsGrounded)
        {
            // Escribir la [Networked] para que los proxies vean el trigger.
            // En el host esta es la escritura autoritativa; en el InputAuthority
            // es una predicción que Fusion sobrescribe con el valor del server,
            // pero los proxies igual ven el cambio cuando llega el snapshot.
            NetJumpTick = Runner.Tick;

            // Disparo local para el owner. NO usamos _lastJumpTick acá: el owner
            // NUNCA lee NetJumpTick en Render(), así que no hay riesgo de doble
            // disparo. (Es el bug que tenía la versión anterior).
            animator.SetTrigger(JumpTriggerHash);

            ResetIdleLocal();
        }

        // MELEE
        if (input.Buttons.WasPressed(_previousButtons, InputButton.BasicAttack))
        {
            NetMeleeTick = Runner.Tick;
            animator.SetTrigger(MeleeTrigger);
            ResetIdleLocal();
        }

        // RANGE (FirstSkill)
        if (input.Buttons.WasPressed(_previousButtons, InputButton.FirstSkill))
        {
            NetRangeTick = Runner.Tick;
            animator.SetTrigger(RangeTrigger);
            ResetIdleLocal();
        }

        // ── BOOLEANOS PREDICHOS ─────────────────────────────────────────────
        // isWalking e isGrounded los escribimos como [Networked]. Tanto host como
        // InputAuthority escriben — en el cliente queda como predicción local.
        UpdateMovementFlags(input.Direction);

        // ── IDLE RANDOMIZER (NO predicho) ──────────────────────────────────
        // Usa Random.value, que es no-determinístico — si lo corriéramos en el
        // InputAuthority, el cliente y el server elegirían variantes distintas.
        if (HasStateAuthority)
        {
            UpdateIdleRandomizerServer();
        }

        _previousButtons = input.Buttons;
    }

    private void UpdateMovementFlags(Vector2 inputDir)
    {
        bool dashing = controller != null && controller.IsDashing;
        bool grounded = kcc.IsGrounded;
        float vertVel = controller != null ? controller.NetVerticalVelocity : 0f;

        // isFalling: no está en suelo Y está bajando con velocidad significativa.
        // Esto captura caídas de montañas, post-dash en aire, post-salto, etc.
        bool falling = !grounded && vertVel < FallVelocityThreshold && !dashing;

        // isWalking: hay input de dirección, está en suelo y no está dasheando.
        // El chequeo de grounded evita que "walk" quede activo al caer de una montaña.-
        bool walking = inputDir.magnitude > 0.1f && grounded && !dashing;

        NetIsWalking = walking;
        NetIsGrounded = grounded;
        NetIsFalling = falling;
    }

    private void UpdateIdleRandomizerServer()
    {
        bool dashing = controller != null && controller.IsDashing;

        if (!kcc.IsGrounded || kcc.RealVelocity.sqrMagnitude > 0.5f || dashing)
        {
            ResetIdleServer();
            return;
        }

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
                    int choice = Random.Range(1, 3);
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

    private void ResetIdleServer()
    {
        if (NetIdleType != 0)
            NetIdleType = 0;

        _serverIsInVariant = false;
        _serverIdleTimer = 2f;
    }

    private void ResetIdleLocal()
    {
        if (HasStateAuthority)
        {
            ResetIdleServer();
        }
        else if (NetIdleType != 0)
        {
            NetIdleType = 0;
        }
    }

    /// <summary>
    /// Render corre en TODOS los peers a framerate de pantalla.
    /// 
    /// CAMINO DEL PROXY (no es owner): leemos las [Networked] tick stamps y
    /// disparamos los triggers cuando cambian. Esta es la ÚNICA forma en que
    /// el proxy se entera de los triggers.
    /// 
    /// CAMINO DEL OWNER (host o input authority): NO leemos las [Networked] tick
    /// stamps acá. Ya disparamos los triggers en FUN basándonos en el input.
    /// Si los leyéramos acá también, los triggers se dispararían dos veces
    /// (síntoma: animación de salto/melee/range "doble" desde otra perspectiva).
    /// 
    /// Los booleanos (isWalking, isDashing, isGrounded, idleType) sí se aplican
    /// para todos los peers, porque SetBool/SetInteger son idempotentes y
    /// pueden llamarse múltiples veces sin problema.
    /// </summary>
    public override void Render()
    {
        if (animator == null) return;

        bool dashing = controller != null && controller.IsDashing;

        // Booleanos: aplicar siempre, son idempotentes.
        animator.SetBool(IsWalking, NetIsWalking);
        animator.SetBool(IsDashing, dashing);
        animator.SetBool(IsGrounded, NetIsGrounded);
        animator.SetBool(IsFallingHash, NetIsFalling);
        animator.SetInteger(IdleTypeHash, NetIdleType);

if (controller != null)
    animator.SetFloat(VerticalVelocityHash, controller.NetVerticalVelocity);

// Triggers: SOLO los proxies los leen acá. El owner ya los disparó en FUN.
if (IsOwner) return;

if (NetJumpTick != _lastJumpTick)
{
    _lastJumpTick = NetJumpTick;
    animator.SetTrigger(JumpTriggerHash);
}

if (NetMeleeTick != _lastMeleeTick)
{
    _lastMeleeTick = NetMeleeTick;
    animator.SetTrigger(MeleeTriggerHash);
}

if (NetRangeTick != _lastRangeTick)
{
    _lastRangeTick = NetRangeTick;
    animator.SetTrigger(RangeTriggerHash);
}
}

    // Llamado por el Animation Event en ani_player_jumpStart.
    // Actualmente no-op, se mantiene para silenciar el warning "has no receiver".
    private void FinalizeJump() { }
}