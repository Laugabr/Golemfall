using Fusion;
using Fusion.Addons.SimpleKCC;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

/* 
  CharacterControleler
 
  Handles all player-controlled movement: walking, jumping, and dashing.
  Integrates with Fusion's network input system and SimpleKCC for physics-based movement.
  Disables the camera for non-authoritative clients.
 */

public class CharacterController : NetworkBehaviour
{
    [Header ("Camera Controller")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private SimpleKCC kcc; //kcc: kinematic character controller
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpPower = 10f;
    [Networked] private NetworkButtons PreviousButtons { get; set; } // Tracks previous input state for button checks
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private CharacterStats charStats;

    [Header ("Inventory")]
    [SerializeField] private CharacterPickUp charPickUp;
    
    [Header ("Abilities")]
    [SerializeField] private AbilityHolder charAbilities;


    [Header("Dash")]
    private bool isDashing = false; // Dash state flag
    private float dashTimer = 0f; // Remaining dash time
    private float dashCooldownTimer = 0f; // Cooldown before dash can be used again
    private Vector3 dashDirection; // Direction of the current dash
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    
    public override void Spawned()
    {
        // Apply stronger gravity for snappier movement

        if (!GetComponent<CharacterPickUp>())
        {
            Debug.LogError("Character Pick Up module not found in player");
        }
        else
        {
            charPickUp = GetComponent<CharacterPickUp>();
        }

        kcc.SetGravity(Physics.gravity.y * 2f);

        // Only the owning player keeps the camera; remote clients don't

        if (HasInputAuthority) return;

        DestroyCameraMachine();
    }


    private void OnEnable()
    {
        charStats = GetComponent<CharacterStats>();
    }
    public override void FixedUpdateNetwork() //executing logic that affects gameplay
    {
        if (!GetInput(out NetInputPlayer input)) //gets the input of each client 
        return;
        
            Vector3 worldDirection = kcc.TransformRotation * new Vector3(input.Direction.x, 0f, input.Direction.y); //take the kcc transform rotation and we multiply it by the direction of the input
            float jump = 0f;
        
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && kcc.IsGrounded) //check if player pressed jump button and is grounded
        {
            jump = jumpPower;
        }
            
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Dash) && dashCooldownTimer <= 0f && input.Direction.magnitude > 0.1f)
        {
            StartDash(input.Direction);
        }


        if  (input.Buttons.IsSet(NetInputPlayer.MOUSE_BUTTON_0)) //If is set as true, spawn projectile
        {
            //attack
        }
        
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Interact))
        {
            Debug.Log(Object + " Calls TryPickUp");
            charPickUp.TryPickUp();
        }

            kcc.Move(worldDirection.normalized * charStats.GetStat(Stat.speed), jump); //normalizing the wD vector to prevent cheating

            PreviousButtons = input.Buttons;

        if (isDashing)
        {
            HandleDashMovement();
            return; // If dashing, override normal movement
        }
    }
    
    private void HandleDashMovement()
    {
        if (GetInput(out NetInputPlayer input)) //gets the input of each client

        kcc.Move(input.Direction * dashSpeed * Time.deltaTime);
        dashTimer -= Time.deltaTime;

         // Stop dash when timer runs out
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
        dashDirection = moveDir.normalized; // Store dash direction
    }

    private void DestroyCameraMachine()
    {
        // Remove Cinemachine camera for non-authoritative players

        var cineMachine = GetComponentInChildren<CinemachineCamera>();
        Destroy(cineMachine.gameObject);
    }


}

