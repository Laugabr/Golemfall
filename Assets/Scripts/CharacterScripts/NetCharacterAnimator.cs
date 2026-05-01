using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

/// <summary>
/// Handles animation state for the networked character.
///
/// Fusion 2 networking model:
///   - StateAuthority (host) computes the animation state from input and physics.
///   - Networked variables replicate that state to InputAuthority and Proxies.
///   - All peers (host, owner, proxies) read those Networked variables in Render()
///     and drive the Animator. This guarantees remote players see animations.
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

    [Header("Idle Settings")]
    [SerializeField] private float chanceToChange = 0.15f;
    [SerializeField] private float variantDuration = 4f;

    // ─────────────────────────────────────────────────────────────────────────
    // Networked state — readable by all peers (host, owner, proxies).
    // The StateAuthority writes these; everyone else just reads them.
    // ─────────────────────────────────────────────────────────────────────────

    [Networked] private NetworkBool NetIsWalking { get; set; }
    [Networked] private NetworkBool NetIsDashing { get; set; }
    [Networked] private NetworkBool NetIsGrounded { get; set; }
    [Networked] private int NetIdleType { get; set; }

    // Trigger replication via "tick stamp": when the stamp changes, fire the trigger once.
    [Networked] private int NetJumpTick { get; set; }
    [Networked] private int NetMeleeTick { get; set; }
    [Networked] private int NetRangeTick { get; set; }

    // Last tick we observed locally — used to detect changes in Render().
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

    // Server-only state for the dash timer and idle randomizer.
    private NetworkButtons _previousButtons;
    private bool _serverIsDashing;
    private float _serverDashTimer;
    private float _serverIdleTimer;
    private bool _serverIsInVariant;

    // Keep in sync with NetCharacterController
    private const float DashDuration = 0.2f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (kcc == null)
            kcc = GetComponent<SimpleKCC>();
    }

    public override void Spawned()
    {
        // Initialize last-seen ticks so we don't fire stale triggers when
        // a late-joining proxy spawns into a state with old stamps.
        _lastJumpTick = NetJumpTick;
        _lastMeleeTick = NetMeleeTick;
        _lastRangeTick = NetRangeTick;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FixedUpdateNetwork: only the StateAuthority writes Networked state.
    // ─────────────────────────────────────────────────────────────────────────
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (animator == null) return;

        // Read input to drive triggers and walking state.
        // GetInput() works on StateAuthority for the player whose input we own.
        if (!GetInput(out NetInputPlayer input))
        {
            // No input this tick (can happen briefly): still update grounded/walking
            // based on physics so proxies don't freeze.
            UpdateMovementFlags(Vector3.zero);
            return;
        }

        // Dash timing (server-side)
        bool dashPressed = input.Buttons.WasPressed(_previousButtons, InputButton.Dash);
        bool hasDirection = input.Direction.magnitude > 0.1f;

        if (dashPressed && hasDirection)
        {
            _serverIsDashing = true;
            _serverDashTimer = DashDuration;
            ResetIdleServer();
        }

        if (_serverIsDashing)
        {
            _serverDashTimer -= Runner.DeltaTime;
            if (_serverDashTimer <= 0f)
                _serverIsDashing = false;
        }

        // Jump
        bool jumpPressed = input.Buttons.WasPressed(_previousButtons, InputButton.Jump);
        if (jumpPressed && kcc.IsGrounded)
        {
            NetJumpTick = Runner.Tick;
            ResetIdleServer();
        }

        // Attacks: replicate via tick stamp
        if (input.Buttons.WasPressed(_previousButtons, InputButton.BasicAttack))
        {
            NetMeleeTick = Runner.Tick;
            ResetIdleServer();
        }

        if (input.Buttons.WasPressed(_previousButtons, InputButton.FirstSkill))
        {
            NetRangeTick = Runner.Tick;
            ResetIdleServer();
        }

        // Movement / grounded flags
        UpdateMovementFlags(input.Direction);

        // Idle randomizer (server-side, replicated via NetIdleType)
        UpdateIdleRandomizerServer();

        _previousButtons = input.Buttons;
    }

    private void UpdateMovementFlags(Vector3 inputDir)
    {
        bool hasDir = inputDir.magnitude > 0.1f;
        NetIsWalking = hasDir && !_serverIsDashing;
        NetIsDashing = _serverIsDashing;
        NetIsGrounded = kcc.IsGrounded;
    }

    private void UpdateIdleRandomizerServer()
    {
        // Cancel variants if moving / airborne / dashing
        if (!kcc.IsGrounded || kcc.RealVelocity.sqrMagnitude > 0.5f || _serverIsDashing)
        {
            ResetIdleServer();
            return;
        }

        // Cancel if currently in an "Attack" tagged state on the host's animator
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
                    int choice = Random.Range(1, 3); // 1 or 2
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

    // ─────────────────────────────────────────────────────────────────────────
    // Render: runs on every peer (host, owner, proxies) at display framerate.
    // This is where we drive the Animator from the Networked state.
    // ─────────────────────────────────────────────────────────────────────────
    public override void Render()
    {
        if (animator == null) return;

        // Booleans
        animator.SetBool(IsWalking, NetIsWalking);
        animator.SetBool(IsDashing, NetIsDashing);
        animator.SetBool(IsGrounded, NetIsGrounded);
        animator.SetInteger(IdleTypeHash, NetIdleType);

        // Triggers via tick-stamp diff
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

    private void FinalizeJump() { }
}