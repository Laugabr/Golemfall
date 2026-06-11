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
    [Networked] private float DashTimer { get; set; }
    [Networked] private float DashCooldownTimer { get; set; }
    [Networked] private Vector3 DashDirection { get; set; }

    [Header("Health")]
    [SerializeField] private PlayerHealth charHealth;
    public bool IsDead => charHealth != null && charHealth.IsDead;

    [SerializeField] private int playerIndex;

    /// <summary>
    /// Botones del tick anterior usados para detectar flancos (WasPressed).
    ///
    /// DEBE ser [Networked] para sobrevivir rollbacks. Si fuera una variable
    /// local normal, Fusion no la restauraría al resimular ticks pasados, lo que
    /// causaría que WasPressed devuelva resultados incorrectos durante la
    /// resimulación. Eso se manifestaba como tirones al dashear o saltar: el
    /// cliente predecía una acción, el host la calculaba distinto, y la
    /// reconciliación producía un salto de posición visible.
    /// </summary>
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    // Cache local — solo se usa en el cliente con InputAuthority.
    private InventoryToggle cachedInventoryToggle;
    private Camera cachedMainCamera;
    private CameraController cachedCameraController;

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
    [Networked] private NetworkBool IsAttacking { get; set; }

    private void Awake()
    {
        charStats = GetComponent<CharacterStats>();
        charPickUp = GetComponent<CharacterPickUp>();
        charAbilities = GetComponent<AbilityHolder>();
        charHealth = GetComponent<PlayerHealth>();
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

        if (DashCooldownTimer > 0f)
            DashCooldownTimer -= Runner.DeltaTime;

        float yaw = input.CameraYaw;
        Vector3 camForward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 camRight = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        Vector3 inputWorld = camForward * input.Direction.y + camRight * input.Direction.x;

        if (inputWorld.sqrMagnitude > 1f)
            inputWorld.Normalize();

        // ── RESPAWN ─────────────────────────────────────────────────────────────
        if (charHealth != null && charHealth.NeedsRespawn)
        {
            if (Object.HasStateAuthority)
            {
                kcc.SetPosition(charHealth._lastSpawnPoint);
                charHealth.NeedsRespawn = false;
            }
            return;
        }

        // ── MUERTO (sin respawn pendiente) ──────────────────────────────────────
        if (charHealth != null && charHealth.IsDead)
        {
            kcc.Move(Vector3.zero);

            if (HasStateAuthority && Runner.DeltaTime > 0f)
                NetVerticalVelocity = (transform.position.y - previousY) / Runner.DeltaTime;
            return;
        }

        // ── DASH ────────────────────────────────────────────────────────────────
        // Se activa solo si: hay flanco de subida en el botón, no hay cooldown y
        // hay dirección de movimiento.
        // WasPressed usa PreviousButtons (networked) para detectar el flanco de
        // forma correcta tanto en simulación normal como en resimulaciones de
        // rollback — si PreviousButtons fuera una variable local, no sobreviviría
        // el rollback y WasPressed daría resultados incorrectos.
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Dash)
            && DashCooldownTimer <= 0f
            && input.Direction.magnitude > 0.1f)
        {
            IsDashing = true;
            DashTimer = dashDuration;
            DashCooldownTimer = dashCooldown;
            DashDirection = inputWorld.normalized;
        }

        // ── SALTO ───────────────────────────────────────────────────────────────
        // Igual que el dash: WasPressed necesita PreviousButtons networked para
        // que el flanco se detecte correctamente durante resimulaciones.
        float jump = 0f;
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && kcc.IsGrounded)
            jump = jumpPower;

        // ── HABILIDADES / INTERACCIÓN ────────────────────────────────────────────
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.BasicAttack) ||
            input.Buttons.WasPressed(PreviousButtons, InputButton.MouseButton0))
        {
            if (HasInputAuthority && !IsInventoryOpen())
            {
                Vector3 mouseDir = GetMouseDirection();

                if (mouseDir.sqrMagnitude > 0.01f && (charAbilities == null || charAbilities.IsReady(0)))
                {
                    NetBodyYaw = Mathf.Atan2(mouseDir.x, mouseDir.z) * Mathf.Rad2Deg;
                    IsAttacking = true;
                }

                charAbilities?.RPC_RequestUseAbility(0, mouseDir);
            }
        }

        if (input.Buttons.WasPressed(PreviousButtons, InputButton.FirstSkill) && HasInputAuthority)
        {
            Vector3 mouseDir = GetMouseDirection();

            if (mouseDir.sqrMagnitude > 0.01f && (charAbilities == null || charAbilities.IsReady(1)))
            {
                NetBodyYaw = Mathf.Atan2(mouseDir.x, mouseDir.z) * Mathf.Rad2Deg;
                IsAttacking = true;
            }

            charAbilities?.RPC_RequestUseAbility(1, mouseDir);
        }

        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Interact) && HasInputAuthority)
            charPickUp?.TryPickUp();

        // ── MOVIMIENTO ───────────────────────────────────────────────────────────
        Vector3 moveDir;

        if (IsDashing)
        {
            Vector3 flatDirection = new Vector3(DashDirection.x, 0f, DashDirection.z).normalized;

            // Cancelamos la velocidad vertical para que el dash sea completamente
            // horizontal independientemente del estado de salto o caída.
            kcc.Move(flatDirection * dashSpeed, -kcc.RealVelocity.y);

            DashTimer -= Runner.DeltaTime;
            if (DashTimer <= 0f)
                IsDashing = false;

            moveDir = flatDirection;
        }
        else
        {
            kcc.Move(inputWorld * charStats.GetStat(Stat.speed), jump);
            moveDir = inputWorld;
        }

        // ── YAW REPLICADO (PREDICTION) ───────────────────────────────────────────
        // Tanto el StateAuthority como el InputAuthority escriben este valor.
        // En el cliente con InputAuthority, la escritura es predicción local que
        // Fusion reconcilia con el snapshot del host. El personaje local ve su
        // rotación responder inmediatamente sin esperar el round-trip al server.
        // Si no hay dirección este tick, conservamos el yaw anterior — el
        // personaje queda mirando hacia donde venía caminando.
        if (moveDir.sqrMagnitude > 0.01f && !IsAttacking)
            NetBodyYaw = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;

        if (HasStateAuthority && Runner.DeltaTime > 0f)
            NetVerticalVelocity = (transform.position.y - previousY) / Runner.DeltaTime;

        // Guardamos los botones de este tick para detectar flancos en el siguiente.
        // Al ser [Networked], Fusion los restaura correctamente durante rollbacks.
        PreviousButtons = input.Buttons;
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

    public void ClearAttackLock()
    {
        IsAttacking = false;
    }
}