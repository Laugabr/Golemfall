using Fusion;
using Fusion.Addons.SimpleKCC;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class CharacterMovement : NetworkBehaviour
{
    [Header ("Camera Controller")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private SimpleKCC kcc; //kcc: kinematic character controller
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpPower = 10f;
    [Networked] private NetworkButtons PreviousButtons { get; set; }
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private CharacterStats charStats;

    [Header("Dash")]
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    
    public override void Spawned()
    {
        kcc.SetGravity(Physics.gravity.y * 2f);

        if (HasInputAuthority) return;

        DestroyCameraMachine();
    }


    private void OnEnable()
    {
        charStats = GetComponent<CharacterStats>();
    }
    public override void FixedUpdateNetwork() //executing logic that affects gameplay
    {
        if (GetInput(out NetInputPlayer input)) //gets the input of each client
        {
            Vector3 worldDirection = kcc.TransformRotation * new Vector3(input.Direction.x, 0f, input.Direction.y); //take the kcc transform rotation and we multiply it by the direction of the input
            float jump = 0f;

            if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && kcc.IsGrounded) //check if player pressed jump button and is grounded
            {
                jump = jumpPower;
            }
            
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f && input.Direction.magnitude > 0.1f)
        {
            StartDash(input.Direction);
        }
            kcc.Move(worldDirection.normalized * charStats.GetStat(Stat.speed), jump); //normalizing the wD vector to prevent cheating

            PreviousButtons = input.Buttons;
        }

        if (isDashing)
        {
            HandleDashMovement();
            return; // no permitir movimiento normal durante el dash
        }
    }
    
        private void HandleDashMovement()
    {
        if (GetInput(out NetInputPlayer input)) //gets the input of each client

        kcc.Move(input.Direction * dashSpeed * Time.deltaTime);
        dashTimer -= Time.deltaTime;

        if (dashTimer <= 0f)
        {
            isDashing = false;
        }
    }
        private void StartDash(Vector3 moveDir)
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDirection = moveDir.normalized;
    }

    private void DestroyCameraMachine() {
        var cineMachine = GetComponentInChildren<CinemachineCamera>();
        Destroy(cineMachine.gameObject);
    }
}

