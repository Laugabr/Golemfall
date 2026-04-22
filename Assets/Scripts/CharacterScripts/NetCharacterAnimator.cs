using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using UnityEngine.Windows;

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

    [Header("Idle Settings")]
    [SerializeField] private float chanceToChange = 0.15f;
    [SerializeField] private float variantDuration = 4f;
    private float _idleTimer = 0f;
    private bool _isInVariant = false;
    private static readonly int IdleTypeHash = Animator.StringToHash("idleType");

    // Cached parameter hashes (faster than string lookup every tick)
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private static readonly int IsDashingHash = Animator.StringToHash("isDashing");
    private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
    private static readonly int JumpTrigger = Animator.StringToHash("jumpTrigger");
    private static readonly int MeleeTrigger = Animator.StringToHash("meleeTrigger");
    private static readonly int RangeTrigger = Animator.StringToHash("rangeTrigger");

    [Networked] public bool IsDashing { get; set; }
    [Networked] public bool IsWalking { get; set; }
    [Networked] public bool IsGrounded { get; set; }
    [Networked] public bool IsJumping { get; set; }
    [Networked] public bool IsMelee { get; set; }
    [Networked] public bool IsRange { get; set; }


    private NetworkButtons _previousButtons;
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
        if (!Object.HasInputAuthority) return;
        if (animator == null) return;

        if (!GetInput(out NetInputPlayer input)) return;

        UpdateIdleRandomizer();

        //  Dash 
        bool dashPressed = input.Buttons.WasPressed(_previousButtons, InputButton.Dash);
        bool hasDirection = input.Direction.magnitude > 0.1f;

        if (dashPressed && hasDirection)
        {
            IsDashing = true;
            _dashTimer = DashDuration;
            ResetIdleSystem();
        }

        if (IsDashing)
        {
            _dashTimer -= Runner.DeltaTime;
            if (_dashTimer <= 0f)
                IsDashing = false;
        }

        //  Jump 
        bool jumpPressed = input.Buttons.WasPressed(_previousButtons, InputButton.Jump);
        if (jumpPressed && kcc.IsGrounded)
        {
            IsJumping = true;   
        }
        else
        {
            IsJumping = false;
        }

        //  Attacks 
        if (input.Buttons.WasPressed(_previousButtons, InputButton.BasicAttack))
        {
            IsMelee = true;
        }
        else
        {
            IsMelee = false;
        }

        if (input.Buttons.WasPressed(_previousButtons, InputButton.FirstSkill))
        {
            IsRange = true;
        }
        else{
            IsRange = false;
        }

        // Set all bool parameters 


        _previousButtons = input.Buttons;
        IsWalking = hasDirection && !IsDashing;

        SetAnimations();
    }

    private void SetAnimations()
    {
        if (IsJumping)
        {
            animator.SetTrigger(JumpTrigger);
            ResetIdleSystem();
        }

        //  Attacks 
        if (IsMelee)
        {
            animator.SetTrigger(MeleeTrigger);
            ResetIdleSystem();
        }

        if (IsRange)
        {
            animator.SetTrigger(RangeTrigger);
            ResetIdleSystem();
        }

        animator.SetBool(IsWalkingHash, IsWalking);
        animator.SetBool(IsDashingHash, IsDashing);
        animator.SetBool(IsGroundedHash, kcc.IsGrounded);
    }

    private void UpdateIdleRandomizer()
    {
        // 1. Si se mueve, salta o está en dash, cancelamos variantes
        if (!kcc.IsGrounded || kcc.RealVelocity.sqrMagnitude > 0.5f || IsDashing)
        {
            ResetIdleSystem();
            return;
        }

        // 2. Si está en medio de una animación de ataque (usa el Tag "Attack" en el Animator)
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsTag("Attack"))
        {
            ResetIdleSystem();
            return;
        }

        // 3. Lógica del cronómetro
        _idleTimer -= Runner.DeltaTime;

        if (_idleTimer <= 0f)
        {
            if (!_isInVariant)
            {
                if (Random.value < chanceToChange)
                {
                    int choice = Random.Range(1, 3); // Escoge 1 o 2
                    animator.SetInteger(IdleTypeHash, choice);
                    _idleTimer = variantDuration;
                    _isInVariant = true;
                }
                else
                {
                    _idleTimer = 2f; // Reintento corto
                }
            }
            else
            {
                // Volver al reposo y esperar un tiempo largo (paz)
                animator.SetInteger(IdleTypeHash, 0);
                _idleTimer = 5f;
                _isInVariant = false;
            }
        }
    }

    private void ResetIdleSystem()
    {
        // Solo enviamos el parámetro si es necesario (para ahorrar red)
        if (animator.GetInteger(IdleTypeHash) != 0)
            animator.SetInteger(IdleTypeHash, 0);

        _isInVariant = false;
        _idleTimer = 2f; // Bloqueo de seguridad de 2 segundos
    }

    public override void Render()
    {
        SetAnimations();
    }

}