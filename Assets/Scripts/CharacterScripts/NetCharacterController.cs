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

    /// <summary>
    /// Máximo de dashes permitidos en el aire antes de tocar el piso.
    /// Configurable desde el Inspector — 2 por defecto.
    /// </summary>
    [SerializeField] private int maxAirDashes = 2;

    [Networked] private float DashTimer { get; set; }
    [Networked] private float DashCooldownTimer { get; set; }
    [Networked] private Vector3 DashDirection { get; set; }

    /// <summary>
    /// Contador de dashes realizados en el aire desde la última vez que tocó el piso.
    /// Se resetea a 0 cuando kcc.IsGrounded es true.
    /// DEBE ser [Networked] para sobrevivir rollbacks — mismo patrón que DashTimer.
    /// </summary>
    [Networked] private int AirDashCount { get; set; }

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

    // Cache de progresión — se inicializa en Spawned() para no llamar
    // GetComponent cada tick en FixedUpdateNetwork.
    private PlayerProgressionVisuals _progression;

    /// <summary>
    /// Yaw (en grados) deseado para el bodyVisuals. Se actualiza cada tick a
    /// partir de la dirección de movimiento o de ataque del jugador.
    ///
    /// PREDICTION: tanto el StateAuthority como el InputAuthority escriben este
    /// valor en sus respectivos FixedUpdateNetwork. En el cliente con InputAuthority,
    /// la escritura es una predicción local que Fusion sobrescribe automáticamente
    /// cuando llega el snapshot autoritativo del host. Esto hace que el jugador
    /// local vea su rotación inmediatamente al cambiar de dirección o atacar,
    /// sin esperar el round-trip al server.
    /// </summary>
    [Networked] private float NetBodyYaw { get; set; }

    /// <summary>
    /// Estado de dash autoritativo. Tanto el host como el InputAuthority lo activan
    /// cuando se cumplen las condiciones (input + cooldown + dirección + desbloqueado).
    /// En el cliente, esto funciona como predicción local: el dash se ve inmediatamente
    /// y Fusion sincroniza con el host. El NetCharacterAnimator lo lee para
    /// decidir si reproducir la animación de dash.
    /// </summary>
    [Networked] public NetworkBool IsDashing { get; private set; }
    [Networked] public float NetVerticalVelocity { get; private set; }

    /// <summary>
    /// Indica que el jugador está en medio de un ataque y no debe rotar
    /// por movimiento. Se resetea via ClearAttackLock() llamado desde
    /// un Animation Event al terminar la animación de ataque.
    /// </summary>
    [Networked] private NetworkBool IsAttacking { get; set; }
    private CapsuleCollider capsuleCollider;
    private void Awake()
    {
        charStats = GetComponent<CharacterStats>();
        charPickUp = GetComponent<CharacterPickUp>();
        charAbilities = GetComponent<AbilityHolder>();
        charHealth = GetComponent<PlayerHealth>();
        capsuleCollider = GetComponent<CapsuleCollider>();

    }

    public override void Spawned()
    {
        kcc.SetGravity(Physics.gravity.y * 3f);

        if (HasStateAuthority && bodyVisuals != null)
            NetBodyYaw = bodyVisuals.eulerAngles.y;

        // Cacheamos la progresión para chequear desbloqueos sin llamar
        // GetComponent cada tick en FixedUpdateNetwork.
        _progression = GetComponent<PlayerProgressionVisuals>();

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

            // Registramos el transform del jugador local en el NetworkInputManager
            // para que pueda calcular AttackYaw relativo a la posición real del jugador.
            var inputManager = FindFirstObjectByType<NetworkInputManager>();
            if (inputManager != null)
                inputManager.SetLocalPlayer(transform);
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
        // Resetea el contador de dashes en el aire al tocar el piso.
        // Así el jugador puede volver a dashear en el aire después de aterrizar.
        if (kcc.IsGrounded)
            AirDashCount = 0;

        bool dashUnlocked = _progression == null || _progression.IsDashUnlocked();

        // Permite dashear si está en el piso O si no superó el límite de dashes en el aire.
        // Esto da hasta maxAirDashes dashes consecutivos en el aire antes de tener
        // que tocar el piso para resetear el contador.
        bool canAirDash = kcc.IsGrounded || AirDashCount < maxAirDashes;

        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Dash)
            && DashCooldownTimer <= 0f
            && input.Direction.magnitude > 0.1f
            && dashUnlocked
            && canAirDash)
        {
            // Si está en el aire incrementamos el contador antes de dashear
            if (!kcc.IsGrounded)
                AirDashCount++;

            IsDashing = true;
            DashTimer = dashDuration;
            DashCooldownTimer = dashCooldown;
            DashDirection = inputWorld.normalized;
        }

        // ── SALTO ───────────────────────────────────────────────────────────────
        // WasPressed necesita PreviousButtons networked para que el flanco se
        // detecte correctamente durante resimulaciones de rollback.
        float jump = 0f;
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && kcc.IsGrounded)
            jump = jumpPower;

        // ── HABILIDADES / INTERACCIÓN ────────────────────────────────────────────

        // MELEE (BasicAttack) — siempre disponible, sin chequeo de progresión.
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

                charAbilities?.RPC_RequestUseAbility(0, mouseDir, input.AttackYaw);
            }
        }

        // RANGE (FirstSkill) — bloqueado hasta que se desbloquee por progresión.
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.FirstSkill)
            && HasInputAuthority
            && (_progression == null || _progression.IsAbilityUnlocked(1)))
        {
            Vector3 mouseDir = GetMouseDirection();

            if (mouseDir.sqrMagnitude > 0.01f && (charAbilities == null || charAbilities.IsReady(1)))
            {
                NetBodyYaw = Mathf.Atan2(mouseDir.x, mouseDir.z) * Mathf.Rad2Deg;
                IsAttacking = true;
            }

            charAbilities?.RPC_RequestUseAbility(1, mouseDir, input.AttackYaw);
        }

        // HEAL (SecondarySkill) — bloqueado hasta que se desbloquee por progresión.
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.SecondarySkill)
            && HasInputAuthority
            && (_progression == null || _progression.IsAbilityUnlocked(2)))
        {
            charAbilities?.RPC_RequestUseAbility(2, Vector3.zero, 0f);
        }

        // PICK UP (Interact) — siempre disponible.
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
        // Si el jugador está atacando, conservamos el yaw del ataque — no
        // sobreescribimos con la dirección de movimiento hasta que ClearAttackLock()
        // libere IsAttacking al terminar la animación.
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
                if(HasInputAuthority)
            Shader.SetGlobalVector("_Player", transform.position + Vector3.up * capsuleCollider.radius);
    }

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

    /// <summary>
    /// Llamado desde AbilityHolder.RPC_RequestUseAbility() en el host para
    /// escribir NetBodyYaw con el yaw de ataque del cliente, antes de que
    /// la habilidad pase a Cooldown.
    /// </summary>
    public void SetAttackYaw(float yaw)
    {
        NetBodyYaw = yaw;
        IsAttacking = true;
    }

    /// <summary>
    /// Libera el lock de ataque para que el personaje pueda volver a rotar
    /// según la dirección de movimiento. Llamado desde un Animation Event
    /// al finalizar la animación de ataque.
    /// </summary>
    public void ClearAttackLock()
    {
        IsAttacking = false;
    }
}