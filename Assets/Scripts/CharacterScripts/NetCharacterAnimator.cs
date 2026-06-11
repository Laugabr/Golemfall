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
/// Animator parameters expected:
///   bool    isWalking
///   bool    isDashing
///   bool    isGrounded
///   bool    isFalling
///   bool    isDead
///   int     idleType
///   float   verticalVelocity
///   trigger jumpTrigger
///   trigger meleeTrigger
///   trigger rangeTrigger
///   trigger takeDamageTrigger
///   trigger deathTrigger
/// </summary>
public class NetCharacterAnimator : NetworkBehaviour
{
    [SerializeField] private SimpleKCC kcc;
    [SerializeField] private Animator animator;
    [SerializeField] private NetCharacterController controller;
    [SerializeField] private AbilityHolder abilityHolder;

    [Header("Idle Settings")]
    [SerializeField] private float chanceToChange = 0.15f;
    [SerializeField] private float variantDuration = 4f;

    // ── Variables networked — leídas por proxies en Render() ─────────────────
    [Networked] private NetworkBool NetIsWalking { get; set; }
    [Networked] private NetworkBool NetIsGrounded { get; set; }
    [Networked] private NetworkBool NetIsFalling { get; set; }
    [Networked] private int NetIdleType { get; set; }

    // DEBE ser [Networked] para sobrevivir rollbacks. Mismo patrón que
    // PreviousButtons en NetCharacterController — si fuera variable local,
    // WasPressed daría resultados incorrectos durante resimulaciones y las
    // animaciones de ataque no se dispararían en el cliente.
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    // Tick stamps para triggers — cuando cambian, el proxy dispara el trigger una vez
    [Networked] private int NetJumpTick { get; set; }
    [Networked] private int NetMeleeTick { get; set; }
    [Networked] private int NetRangeTick { get; set; }

    /// <summary>
    /// Tick stamp para el trigger de recibir daño.
    /// Solo el StateAuthority lo escribe via TriggerTakeDamage().
    /// Los proxies lo leen en Render() y disparan el trigger cuando cambia.
    /// El owner (host) lo dispara directamente en TriggerTakeDamage().
    /// </summary>
    [Networked] private int NetTakeDamageTick { get; set; }

    // Último tick observado por los PROXIES — detecta cambios en Render()
    private int _lastJumpTick;
    private int _lastMeleeTick;
    private int _lastRangeTick;
    private int _lastTakeDamageTick;

    // Hashes cacheados de los parámetros del Animator
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsDashing = Animator.StringToHash("isDashing");
    private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int IsFallingHash = Animator.StringToHash("isFalling");
    private static readonly int IdleTypeHash = Animator.StringToHash("idleType");
    private static readonly int JumpTriggerHash = Animator.StringToHash("jumpTrigger");
    private static readonly int MeleeTriggerHash = Animator.StringToHash("meleeTrigger");
    private static readonly int RangeTriggerHash = Animator.StringToHash("rangeTrigger");
    private static readonly int TakeDamageTriggerHash = Animator.StringToHash("takeDamageTrigger");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("verticalVelocity");
    private static readonly int DeathTriggerHash = Animator.StringToHash("deathTrigger");
    private static readonly int IsDeadHash = Animator.StringToHash("isDead");

    // Estado del idle randomizer — SOLO en StateAuthority (es no-determinístico)
    private float _serverIdleTimer;
    private bool _serverIsInVariant;

    private bool _wasDashingLastTick;
    private bool _wasDeadLastFrame;

    private const float FallVelocityThreshold = -1.5f;
    private bool IsOwner => HasInputAuthority || HasStateAuthority;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (kcc == null)
            kcc = GetComponent<SimpleKCC>();

        if (controller == null)
            controller = GetComponent<NetCharacterController>();

        if (abilityHolder == null)
            abilityHolder = GetComponent<AbilityHolder>();
    }

    public override void Spawned()
    {
        // Inicializar last-seen ticks para que un proxy que entra tarde no
        // dispare triggers viejos al recibir el primer snapshot.
        _lastJumpTick = NetJumpTick;
        _lastMeleeTick = NetMeleeTick;
        _lastRangeTick = NetRangeTick;
        _lastTakeDamageTick = NetTakeDamageTick;
    }

    public override void FixedUpdateNetwork()
    {
        if (animator == null) return;

        if (!GetInput(out NetInputPlayer input)) return;

        // Si está muerto no procesamos ningún input de animación
        if (controller != null && controller.IsDead)
        {
            PreviousButtons = input.Buttons;
            return;
        }

        // Solo aplicar cambios al Animator durante ticks Forward.
        // Durante resimulación los triggers se dispararían múltiples veces.
        if (!Runner.IsForward)
        {
            PreviousButtons = input.Buttons;
            return;
        }

        // Detectar inicio de dash para resetear el idle
        bool dashing = controller != null && controller.IsDashing;
        bool dashJustStarted = dashing && !_wasDashingLastTick;
        if (dashJustStarted) ResetIdleLocal();
        _wasDashingLastTick = dashing;

        // ── TRIGGERS DEL OWNER ───────────────────────────────────────────────
        // El owner dispara triggers acá. Los proxies los leen en Render().

        // JUMP
        bool jumpPressed = input.Buttons.WasPressed(PreviousButtons, InputButton.Jump);
        if (jumpPressed && kcc.IsGrounded)
        {
            NetJumpTick = Runner.Tick;
            animator.SetTrigger(JumpTriggerHash);
            ResetIdleLocal();
        }

        // MELEE — solo si la habilidad está ready
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.BasicAttack))
        {
            if (abilityHolder == null || abilityHolder.IsReady(0))
            {
                NetMeleeTick = Runner.Tick;
                animator.SetTrigger(MeleeTriggerHash);
                ResetIdleLocal();
            }
        }

        // RANGE — solo si la habilidad está ready
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.FirstSkill))
        {
            if (abilityHolder == null || abilityHolder.IsReady(1))
            {
                NetRangeTick = Runner.Tick;
                animator.SetTrigger(RangeTriggerHash);
                ResetIdleLocal();
            }
        }

        // ── BOOLEANOS ────────────────────────────────────────────────────────
        UpdateMovementFlags(input.Direction);

        // ── IDLE RANDOMIZER (solo StateAuthority — usa Random no-determinístico)
        if (HasStateAuthority)
            UpdateIdleRandomizerServer();

        PreviousButtons = input.Buttons;
    }

    /// <summary>
    /// Llamado por PlayerHealth cuando el jugador recibe daño.
    /// Solo corre en StateAuthority — escribe el tick stamp y dispara
    /// el trigger localmente. Los proxies lo detectan en Render().
    /// </summary>
    public void TriggerTakeDamage()
    {
        if (!Object.HasStateAuthority) return;
        NetTakeDamageTick = Runner.Tick;
        animator.SetTrigger(TakeDamageTriggerHash);
    }

    private void UpdateMovementFlags(Vector2 inputDir)
    {
        bool dashing = controller != null && controller.IsDashing;
        bool grounded = kcc.IsGrounded;
        float vertVel = controller != null ? controller.NetVerticalVelocity : 0f;

        bool falling = !grounded && vertVel < FallVelocityThreshold && !dashing;
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
            ResetIdleServer();
        else if (NetIdleType != 0)
            NetIdleType = 0;
    }

    /// <summary>
    /// Render corre en TODOS los peers a framerate de pantalla.
    /// Booleanos: se aplican para todos los peers (son idempotentes).
    /// Triggers: SOLO los proxies los leen acá. El owner ya los disparó en FUN
    /// o via TriggerTakeDamage().
    /// </summary>
    public override void Render()
    {
        if (animator == null) return;

        bool dashing = controller != null && controller.IsDashing;
        bool isDead = controller != null && controller.IsDead;

        // Booleanos — aplicar siempre, son idempotentes
        animator.SetBool(IsWalking, NetIsWalking);
        animator.SetBool(IsDashing, dashing);
        animator.SetBool(IsGrounded, NetIsGrounded);
        animator.SetBool(IsFallingHash, NetIsFalling);
        animator.SetInteger(IdleTypeHash, NetIdleType);
        animator.SetBool(IsDeadHash, isDead);

        if (controller != null)
            animator.SetFloat(VerticalVelocityHash, controller.NetVerticalVelocity);

        // Trigger de muerte — solo una vez al pasar de vivo a muerto
        if (isDead && !_wasDeadLastFrame)
            animator.SetTrigger(DeathTriggerHash);
        _wasDeadLastFrame = isDead;

        // Triggers — SOLO proxies los leen acá
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

        // TakeDamage — proxies reaccionan al cambio del tick stamp
        if (NetTakeDamageTick != _lastTakeDamageTick)
        {
            _lastTakeDamageTick = NetTakeDamageTick;
            animator.SetTrigger(TakeDamageTriggerHash);
        }
    }

    // Llamado por Animation Event en ani_player_jumpStart.
    // No-op, se mantiene para silenciar el warning.
    private void FinalizeJump() { }
}