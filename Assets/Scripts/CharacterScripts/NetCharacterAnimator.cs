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
///     la confirmación del server.
///   - Los proxies leen las [Networked] en Render().
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
///   trigger healTrigger
///   trigger pickupTrigger
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

    [Networked] private NetworkBool NetIsWalking { get; set; }
    [Networked] private NetworkBool NetIsGrounded { get; set; }
    [Networked] private NetworkBool NetIsFalling { get; set; }
    [Networked] private int NetIdleType { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    [Networked] private int NetJumpTick { get; set; }
    [Networked] private int NetMeleeTick { get; set; }
    [Networked] private int NetRangeTick { get; set; }
    [Networked] private int NetPickupTick { get; set; } // ← NUEVO: tick de la animación de pickup

    /// <summary>
    /// Tick stamp para la animación de curación.
    /// Escrito por el owner (HasInputAuthority || HasStateAuthority).
    /// El StateAuthority también lo escribe via TriggerHealAnimation()
    /// llamado desde AbilityHolder.RPC_RequestUseAbility() para garantizar
    /// que los proxies vean la animación aunque el cliente sea quien inició.
    /// </summary>
    [Networked] private int NetHealTick { get; set; }

    [Networked] private int NetTakeDamageTick { get; set; }

    private int _lastJumpTick;
    private int _lastMeleeTick;
    private int _lastRangeTick;
    private int _lastHealTick;
    private int _lastTakeDamageTick;
    private int _lastPickupTick; // ← NUEVO

    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsDashing = Animator.StringToHash("isDashing");
    private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int IsFallingHash = Animator.StringToHash("isFalling");
    private static readonly int IdleTypeHash = Animator.StringToHash("idleType");
    private static readonly int JumpTriggerHash = Animator.StringToHash("jumpTrigger");
    private static readonly int MeleeTriggerHash = Animator.StringToHash("meleeTrigger");
    private static readonly int RangeTriggerHash = Animator.StringToHash("rangeTrigger");
    private static readonly int HealTriggerHash = Animator.StringToHash("healTrigger");
    private static readonly int PickupTriggerHash = Animator.StringToHash("pickupTrigger"); // ← NUEVO
    private static readonly int TakeDamageTriggerHash = Animator.StringToHash("takeDamageTrigger");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("verticalVelocity");
    private static readonly int DeathTriggerHash = Animator.StringToHash("deathTrigger");
    private static readonly int IsDeadHash = Animator.StringToHash("isDead");

    private PlayerProgressionVisuals _progression;

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
        _progression = GetComponent<PlayerProgressionVisuals>();
    }

    public override void Spawned()
    {
        _lastJumpTick = NetJumpTick;
        _lastMeleeTick = NetMeleeTick;
        _lastRangeTick = NetRangeTick;
        _lastHealTick = NetHealTick;
        _lastTakeDamageTick = NetTakeDamageTick;
        _lastPickupTick = NetPickupTick; // ← NUEVO
    }

    /// <summary>
    /// Llamado por AbilityHolder.RPC_RequestUseAbility() en el servidor
    /// cuando el cliente usa la curación. Escribe NetHealTick con autoridad
    /// para que los proxies detecten el cambio en Render() y disparen la animación.
    /// El servidor también dispara el trigger localmente para verse en el host.
    /// </summary>
    public void TriggerHealAnimation()
    {
        if (!Object.HasStateAuthority) return;
        NetHealTick = Runner.Tick;
        animator.SetTrigger(HealTriggerHash);
        ResetIdleLocal();
    }

    /// <summary>
    /// Llamado por CharacterPickUp.TryPickUp() SOLO cuando hay un item cerca
    /// (currentInteractor != null). A diferencia de Jump/Melee/Range, este trigger
    /// NO se dispara con el input crudo — evita que apretar F "en el aire" interrumpa
    /// otras animaciones. El owner (host o cliente) escribe el tick para que los
    /// proxies lo detecten en Render().
    /// </summary>
    public void TriggerPickupAnimation()
    {
        if (!(HasStateAuthority || HasInputAuthority)) return;
        animator.SetTrigger(PickupTriggerHash);
        ResetIdleLocal();
        NetPickupTick = Runner.Tick;
    }

    public override void FixedUpdateNetwork()
    {
        if (animator == null) return;

        bool gotInput = GetInput(out NetInputPlayer input);
        if (!gotInput) return;

        if (controller != null && controller.IsDead)
        {
            PreviousButtons = input.Buttons;
            return;
        }

        if (!Runner.IsForward)
        {
            PreviousButtons = input.Buttons;
            return;
        }

        bool dashing = controller != null && controller.IsDashing;
        bool dashJustStarted = dashing && !_wasDashingLastTick;
        if (dashJustStarted) ResetIdleLocal();
        _wasDashingLastTick = dashing;

        // JUMP
        bool jumpPressed = input.Buttons.WasPressed(PreviousButtons, InputButton.Jump);
        if (jumpPressed && kcc.IsGrounded)
        {
            NetJumpTick = Runner.Tick;
            animator.SetTrigger(JumpTriggerHash);
            ResetIdleLocal();
        }

        // MELEE
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.BasicAttack))
        {
            float cooldownTime = abilityHolder != null ? abilityHolder.GetCooldownTime(0) : 0.7f;
            int cooldownTicks = Mathf.CeilToInt(cooldownTime / Runner.DeltaTime);
            bool animReady = (Runner.Tick - NetMeleeTick) > cooldownTicks;

            if (animReady)
            {
                animator.SetTrigger(MeleeTriggerHash);
                ResetIdleLocal();
                if (HasStateAuthority || HasInputAuthority)
                    NetMeleeTick = Runner.Tick;
            }
        }

        // RANGE
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.FirstSkill))
        {
            bool rangeUnlocked = _progression == null || _progression.IsAbilityUnlocked(1);
            float cooldownTime = abilityHolder != null ? abilityHolder.GetCooldownTime(1) : 0f;
            int cooldownTicks = Mathf.CeilToInt(cooldownTime / Runner.DeltaTime);
            bool animReady = (Runner.Tick - NetRangeTick) > cooldownTicks;

            if (animReady && rangeUnlocked)
            {
                animator.SetTrigger(RangeTriggerHash);
                ResetIdleLocal();
                if (HasStateAuthority || HasInputAuthority)
                    NetRangeTick = Runner.Tick;
            }
        }

        // HEAL
        // El cliente dispara la animación localmente para feedback inmediato.
        // El servidor escribe NetHealTick via TriggerHealAnimation() llamado
        // desde AbilityHolder.RPC_RequestUseAbility() para que los proxies lo vean.
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.SecondarySkill))
        {
            bool healUnlocked = _progression == null || _progression.IsAbilityUnlocked(2);
            float cooldownTime = abilityHolder != null ? abilityHolder.GetCooldownTime(2) : 0f;
            int cooldownTicks = Mathf.CeilToInt(cooldownTime / Runner.DeltaTime);
            bool animReady = (Runner.Tick - NetHealTick) > cooldownTicks;

            if (animReady && healUnlocked && (abilityHolder == null || abilityHolder.IsReady(2)))
            {
                // El owner dispara la animación localmente para verse inmediato.
                // El servidor la dispara via TriggerHealAnimation() para los proxies.
                if (HasInputAuthority)
                {
                    animator.SetTrigger(HealTriggerHash);
                    ResetIdleLocal();
                    NetHealTick = Runner.Tick; // predicción local del cliente
                }
            }
        }

        UpdateMovementFlags(input.Direction);

        if (HasStateAuthority)
            UpdateIdleRandomizerServer();

        PreviousButtons = input.Buttons;
    }

    public void TriggerTakeDamage()
    {
        if (Object.HasStateAuthority)
        {
            NetTakeDamageTick = Runner.Tick;
            animator.SetTrigger(TakeDamageTriggerHash);
            return;
        }

        if (Object.HasInputAuthority)
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
        if (NetIdleType != 0) NetIdleType = 0;
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

    public override void Render()
    {
        if (animator == null) return;

        bool dashing = controller != null && controller.IsDashing;
        bool isDead = controller != null && controller.IsDead;

        animator.SetBool(IsWalking, NetIsWalking);
        animator.SetBool(IsDashing, dashing);
        animator.SetBool(IsGrounded, NetIsGrounded);
        animator.SetBool(IsFallingHash, NetIsFalling);
        animator.SetInteger(IdleTypeHash, NetIdleType);
        animator.SetBool(IsDeadHash, isDead);

        if (controller != null)
            animator.SetFloat(VerticalVelocityHash, controller.NetVerticalVelocity);

        if (isDead && !_wasDeadLastFrame)
            animator.SetTrigger(DeathTriggerHash);
        _wasDeadLastFrame = isDead;

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

        if (NetHealTick != _lastHealTick)
        {
            _lastHealTick = NetHealTick;
            animator.SetTrigger(HealTriggerHash);
        }

        if (NetTakeDamageTick != _lastTakeDamageTick)
        {
            _lastTakeDamageTick = NetTakeDamageTick;
            animator.SetTrigger(TakeDamageTriggerHash);
        }

        if (NetPickupTick != _lastPickupTick) // ← NUEVO
        {
            _lastPickupTick = NetPickupTick;
            animator.SetTrigger(PickupTriggerHash);
        }
    }

    private void FinalizeJump() { }
}