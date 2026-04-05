using Fusion;
using Fusion.Addons.SimpleKCC;
using Unity.Cinemachine;
using UnityEngine;

public class NetCharacterController : NetworkBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Visuals")]
    [SerializeField] private Transform bodyVisuals;

    [Header("Movement")]
    [SerializeField] private SimpleKCC kcc;
    [SerializeField] private float jumpPower = 10f;
    [SerializeField] private CharacterStats charStats;

    [Header("Inventory")]
    [SerializeField] private CharacterPickUp charPickUp;

    [Header("Abilities")]
    [SerializeField] private AbilityHolder charAbilities;

    [Header("Combat")]
    [SerializeField] private PlayerBreaker playerBreaker;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    private NetworkButtons previousButtons;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    private void Awake()
    {
        charStats = GetComponent<CharacterStats>();
        charPickUp = GetComponent<CharacterPickUp>();
        charAbilities = GetComponent<AbilityHolder>();
        playerBreaker = GetComponent<PlayerBreaker>();
    }

    public override void Spawned()
    {
        kcc.SetGravity(Physics.gravity.y * 3f);
        if (!HasInputAuthority)
            DestroyCameraMachine();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetInputPlayer input)) return;
        if (charStats == null || charStats.localStats.Count == 0) return;

        // cooldown
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Runner.DeltaTime;

        // dash start
        if (input.Buttons.WasPressed(previousButtons, InputButton.Dash)
            && dashCooldownTimer <= 0f
            && input.Direction.magnitude > 0.1f)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            dashDirection = (kcc.TransformRotation * new Vector3(
                input.Direction.x, 0f, input.Direction.y)).normalized;
        }

        // jump
        float jump = 0f;
        if (input.Buttons.WasPressed(previousButtons, InputButton.Jump) && kcc.IsGrounded)
            jump = jumpPower;

        // abilities / interact
        if (input.Buttons.WasPressed(previousButtons, InputButton.BasicAttack))
        {
            if (HasInputAuthority)
            {
                playerBreaker?.TryBreak();
                charAbilities?.RPC_RequestUseAbility(0, GetMouseDirection());
            }
        }

        if (input.Buttons.WasPressed(previousButtons, InputButton.FirstSkill) && HasInputAuthority)
            charAbilities?.RPC_RequestUseAbility(1, GetMouseDirection());

        if (input.Buttons.WasPressed(previousButtons, InputButton.SecondarySkill) && HasInputAuthority)
            charAbilities?.RPC_RequestUseAbility(2, GetMouseDirection());

        if (input.Buttons.WasPressed(previousButtons, InputButton.Interact) && HasInputAuthority)
            charPickUp?.TryPickUp();

        // movement
        if (isDashing)
        {
            kcc.Move(dashDirection * dashSpeed);
            dashTimer -= Runner.DeltaTime;
            if (dashTimer <= 0f) isDashing = false;
        }
        else
        {
            Vector3 worldDir = kcc.TransformRotation
                * new Vector3(input.Direction.x, 0f, input.Direction.y);

            kcc.Move(worldDir.normalized * charStats.GetStat(Stat.speed), jump);

            if (worldDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(worldDir);
                bodyVisuals.rotation = Quaternion.Slerp(bodyVisuals.rotation, targetRotation, 15f * Runner.DeltaTime);
            }
        }

        previousButtons = input.Buttons;
    }

    private void DestroyCameraMachine()
    {
        var cam = GetComponentInChildren<CinemachineCamera>();
        if (cam != null) Destroy(cam.gameObject);
    }

    private Vector3 GetMouseDirection()
    {
        if (Camera.main == null) return transform.forward;
        Plane plane = new Plane(Vector3.up, transform.position);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out float dist))
        {
            Vector3 dir = ray.GetPoint(dist) - transform.position;
            dir.y = 0;
            return dir.normalized;
        }
        return transform.forward;
    }
}
