using Fusion;
using Fusion.Addons.SimpleKCC;
using Game.CameraSystem;
using UnityEngine;

public class NetCharacterController : NetworkBehaviour
{
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

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    private NetworkButtons previousButtons;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    // Cache local (solo relevante para el cliente con input authority).
    private InventoryToggle cachedInventoryToggle;
    private Camera cachedMainCamera;

    /// <summary>
    /// Referencia a la CameraController local.
    /// Se cachea en Spawned() y se usa cada tick para leer WorldYaw.
    /// Es null en clientes remotos — el movimiento relativo a la cámara
    /// solo se calcula en el cliente con InputAuthority; el servidor
    /// recibe la dirección ya transformada dentro del input de Fusion.
    /// </summary>
    private CameraController cachedCameraController;

    private void Awake()
    {
        charStats = GetComponent<CharacterStats>();
        charPickUp = GetComponent<CharacterPickUp>();
        charAbilities = GetComponent<AbilityHolder>();
    }

    public override void Spawned()
    {
        kcc.SetGravity(Physics.gravity.y * 3f);

        if (HasInputAuthority)
        {
            cachedMainCamera = Camera.main;
            cachedInventoryToggle = FindFirstObjectByType<InventoryToggle>();

            if (cachedMainCamera != null)
            {
                cachedCameraController = cachedMainCamera.GetComponent<CameraController>();

                if (cachedCameraController != null)
                {
                    cachedCameraController.SetTarget(transform);
                }
                else
                {
                    Debug.LogWarning("[NetCharacterController] Main Camera no tiene CameraController.");
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetInputPlayer input)) return;
        if (charStats == null || charStats.localStats.Count == 0) return;

        //  Cooldown del dash
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Runner.DeltaTime;

        // Dirección de movimiento relativa a la cámara 
        // Construimos los ejes de la cámara proyectados en el plano XZ.
        // Si no hay cámara (clientes remotos o servidor), usamos los ejes del mundo.
        //
        // WASD → input.Direction es un Vector2 (x = strafe, y = avance).
        // Queremos que Y apunte hacia donde mira la cámara (WorldYaw) y X sea
        // su perpendicular hacia la derecha — así W = "hacia la cámara" siempre.
        Vector3 camForward;
        Vector3 camRight;

        if (HasInputAuthority && cachedCameraController != null)
        {
            float yaw = cachedCameraController.WorldYaw;
            camForward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            camRight = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        }
        else
        {
            // Servidor / clientes remotos: usamos los ejes del mundo.
            // No importa porque el servidor no tiene cámara; solo procesa física.
            camForward = Vector3.forward;
            camRight = Vector3.right;
        }

        Vector3 inputWorld = (camForward * input.Direction.y
                            + camRight * input.Direction.x);

        // Normalizamos solo si hay magnitud real para evitar dividir por cero.
        if (inputWorld.sqrMagnitude > 1f)
            inputWorld.Normalize();

        // Inicio del dash 
        if (input.Buttons.WasPressed(previousButtons, InputButton.Dash)
            && dashCooldownTimer <= 0f
            && input.Direction.magnitude > 0.1f)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            dashDirection = inputWorld.normalized;
        }

        // Salto 
        float jump = 0f;
        if (input.Buttons.WasPressed(previousButtons, InputButton.Jump) && kcc.IsGrounded)
            jump = jumpPower;

        // Habilidades / interacción 
        if (input.Buttons.WasPressed(previousButtons, InputButton.BasicAttack) ||
    input.Buttons.WasPressed(previousButtons, InputButton.MouseButton0))
        {
            if (HasInputAuthority && !IsInventoryOpen())
                charAbilities?.RPC_RequestUseAbility(0, GetMouseDirection());
        }

        if (input.Buttons.WasPressed(previousButtons, InputButton.FirstSkill) && HasInputAuthority)
            charAbilities?.RPC_RequestUseAbility(1, GetMouseDirection());

        if (input.Buttons.WasPressed(previousButtons, InputButton.SecondarySkill) && HasInputAuthority)
            charAbilities?.RPC_RequestUseAbility(2, GetMouseDirection());

        if (input.Buttons.WasPressed(previousButtons, InputButton.Interact) && HasInputAuthority)
            charPickUp?.TryPickUp();

        // Movimiento
        if (isDashing)
        {
            kcc.Move(dashDirection * dashSpeed);
            dashTimer -= Runner.DeltaTime;
            if (dashTimer <= 0f) isDashing = false;
        }
        else
        {
            kcc.Move(inputWorld * charStats.GetStat(Stat.speed), jump);

            // Rota el visual del personaje hacia la dirección de movimiento.
            if (inputWorld.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputWorld);
                bodyVisuals.rotation = Quaternion.Slerp(
                    bodyVisuals.rotation, targetRotation, 15f * Runner.DeltaTime
                );
            }
        }

        previousButtons = input.Buttons;
    }

    /// <summary>
    /// Devuelve la dirección normalizada desde el jugador hacia el cursor del mouse
    /// proyectada en el plano XZ. Usado para apuntar habilidades y ataques.
    /// </summary>
    private Vector3 GetMouseDirection()
    {
        Camera cam = cachedMainCamera != null ? cachedMainCamera : Camera.main;
        if (cam == null) return transform.forward;

        Plane plane = new Plane(Vector3.up, transform.position);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (plane.Raycast(ray, out float dist))
        {
            Vector3 dir = ray.GetPoint(dist) - transform.position;
            dir.y = 0f;
            return dir.normalized;
        }
        return transform.forward;
    }

    private bool IsInventoryOpen()
    {
        if (cachedInventoryToggle == null)
            cachedInventoryToggle = FindFirstObjectByType<InventoryToggle>();

        return cachedInventoryToggle != null && cachedInventoryToggle.IsInventoryOpen;
    }
}