using Fusion;
using Fusion.Addons.SimpleKCC;
using Game.CameraSystem;
using UnityEngine;
public class NetCharacterController : NetworkBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Transform bodyVisuals;
    [SerializeField] private float rotationSpeed = 15f;

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

    [Header("Visuals")]

    [SerializeField] private int playerIndex;

    private NetworkButtons previousButtons;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    // Cache local — solo se usa en el cliente con InputAuthority.
    private InventoryToggle cachedInventoryToggle;
    private Camera cachedMainCamera;
    private CameraController cachedCameraController;

    // Networked state que el server escribe y todos los peers leen.

    /// <summary>
    /// Yaw (en grados) deseado para el bodyVisuals. Se actualiza cada tick a
    /// partir de la dirección de movimiento del jugador.
    ///
    /// PREDICTION: tanto el StateAuthority como el InputAuthority escriben este
    /// valor en sus respectivos FixedUpdateNetwork. En el cliente con InputAuthority,
    /// la escritura es una predicción local que Fusion sobrescribe automáticamente
    /// cuando llega el snapshot autoritativo del host. Esto hace que el jugador
    /// local vea su rotación inmediatamente al cambiar de dirección, sin esperar
    /// el round-trip al server.
    /// </summary>
    [Networked] private float NetBodyYaw { get; set; }

    /// <summary>
    /// Estado de dash autoritativo. Tanto el host como el InputAuthority lo activan
    /// cuando se cumplen las condiciones (input + cooldown + dirección). En el
    /// cliente, esto funciona como predicción local: el dash se ve inmediatamente
    /// y Fusion sincroniza con el host. El NetCharacterAnimator lo lee para
    /// decidir si reproducir la animación de dash, evitando que la animación se
    /// dispare cuando el dash real está en cooldown.
    /// </summary>
    [Networked] public NetworkBool IsDashing { get; private set; }
    [Networked] public float NetVerticalVelocity { get; private set; }

    private void Awake()
    {
        charStats = GetComponent<CharacterStats>();
        charPickUp = GetComponent<CharacterPickUp>();
        charAbilities = GetComponent<AbilityHolder>();
    }

    public override void Spawned()
    {
        kcc.SetGravity(Physics.gravity.y * 3f);

        if (HasStateAuthority && bodyVisuals != null)
            NetBodyYaw = bodyVisuals.eulerAngles.y;


        if (HasInputAuthority)
        {
            cachedMainCamera = Camera.main;
            cachedInventoryToggle = FindFirstObjectByType<InventoryToggle>();

            if (cachedMainCamera != null)
            {
                cachedCameraController = cachedMainCamera.GetComponent<CameraController>();
                if (cachedCameraController != null)
                    cachedCameraController.SetTarget(transform);
                else
                    Debug.LogWarning("[NetCharacterController] Main Camera no tiene CameraController.");
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetInputPlayer input)) return;
        if (charStats == null || charStats.localStats.Count == 0) return;

        float previousY = transform.position.y;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Runner.DeltaTime;

        // Dirección de movimiento relativa a la cámara del CLIENTE.
        // El cliente envía su CameraYaw en el input, así que el server puede
        // hacer el cálculo correctamente para CUALQUIER jugador (no solo el local).
        float yaw = input.CameraYaw;
        Vector3 camForward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 camRight   = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        Vector3 inputWorld = camForward * input.Direction.y + camRight * input.Direction.x;

        if (inputWorld.sqrMagnitude > 1f)
            inputWorld.Normalize();

        // Dash start.
        // Se activa solo si: hay input de dash, no hay cooldown y hay dirección.
        // Si está en cooldown, la solicitud se ignora silenciosamente — el
        // animador, al leer IsDashing, NO disparará la animación de dash falsa.
        if (input.Buttons.WasPressed(previousButtons, InputButton.Dash)
            && dashCooldownTimer <= 0f
            && input.Direction.magnitude > 0.1f)
        {
            IsDashing = true;
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

        // Movimiento + actualización de yaw deseado
        Vector3 moveDir;

        if (IsDashing)
        {
            kcc.Move(dashDirection * dashSpeed);
            dashTimer -= Runner.DeltaTime;
            if (dashTimer <= 0f) IsDashing = false;

            moveDir = dashDirection;
        }
        else
        {
            kcc.Move(inputWorld * charStats.GetStat(Stat.speed), jump);
            moveDir = inputWorld;
        }

        // PREDICTION del yaw replicado.
        // Antes solo escribía si HasStateAuthority. Ahora también el InputAuthority
        // escribe — Fusion trata esa escritura como predicción local y la
        // reconcilia con el snapshot autoritativo. Esto hace que el cliente local
        // vea su rotación responder inmediatamente al cambiar de dirección.
        // Si no hay dirección este tick, conservamos el yaw anterior — el
        // personaje queda mirando hacia donde venía caminando.
        if (moveDir.sqrMagnitude > 0.01f)
        {
            NetBodyYaw = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
        }

        if (HasStateAuthority && Runner.DeltaTime > 0f)
        {
            NetVerticalVelocity = (transform.position.y - previousY) / Runner.DeltaTime;
        }

        previousButtons = input.Buttons;
    }

    /// <summary>
    /// Render corre en TODOS los peers (host, owner, proxies) a framerate
    /// de pantalla. Aquí aplicamos el yaw replicado al bodyVisuals con un
    /// Slerp suave para que la rotación se vea fluida en cualquier máquina.
    /// </summary>
    public override void Render()
    {
        if (bodyVisuals == null) return;

        Quaternion target = Quaternion.Euler(0f, NetBodyYaw, 0f);
        bodyVisuals.rotation = Quaternion.Slerp(
            bodyVisuals.rotation, target, rotationSpeed * Time.deltaTime
        );
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