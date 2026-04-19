using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

/// <summary>
/// Handles animation state for the networked character.
/// Reads input and physics state each network tick and drives the Animator.
///
/// Animator parameters expected:
///   bool    isWalking
///   bool    isDashing
///   bool    isGrounded
///   trigger jumpTrigger
///   trigger meleeTrigger
///   trigger rangeTrigger
/// </summary>
public class NetCharacterAnimator : NetworkBehaviour
{
    [SerializeField] private SimpleKCC kcc;
    [SerializeField] private Animator animator;

    // Cached parameter hashes (faster than string lookup every tick)
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsDashing = Animator.StringToHash("isDashing");
    private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int JumpTrigger = Animator.StringToHash("jumpTrigger");
    private static readonly int MeleeTrigger = Animator.StringToHash("meleeTrigger");
    private static readonly int RangeTrigger = Animator.StringToHash("rangeTrigger");

    private NetworkButtons _previousButtons;
    private bool _isDashing;
    private float _dashTimer;

    // Keep these in sync with NetCharacterController values
    private const float DashDuration = 0.2f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (kcc == null)
            kcc = GetComponent<SimpleKCC>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        if (animator == null) return;
        if (!GetInput(out NetInputPlayer input)) return;

        //  Dash 
        bool dashPressed = input.Buttons.WasPressed(_previousButtons, InputButton.Dash);
        bool hasDirection = input.Direction.magnitude > 0.1f;

        if (dashPressed && hasDirection)
        {
            _isDashing = true;
            _dashTimer = DashDuration;
        }

        if (_isDashing)
        {
            _dashTimer -= Runner.DeltaTime;
            if (_dashTimer <= 0f)
                _isDashing = false;
        }

        //  Jump 
        bool jumpPressed = input.Buttons.WasPressed(_previousButtons, InputButton.Jump);
        if (jumpPressed && kcc.IsGrounded)
            animator.SetTrigger(JumpTrigger);

        //  Attacks 
        if (input.Buttons.WasPressed(_previousButtons, InputButton.BasicAttack))
            animator.SetTrigger(MeleeTrigger);

        if (input.Buttons.WasPressed(_previousButtons, InputButton.FirstSkill))
            animator.SetTrigger(RangeTrigger);

        // Set all bool parameters 
        animator.SetBool(IsWalking, hasDirection && !_isDashing);
        animator.SetBool(IsDashing, _isDashing);
        animator.SetBool(IsGrounded, kcc.IsGrounded);

        _previousButtons = input.Buttons;
    }
}