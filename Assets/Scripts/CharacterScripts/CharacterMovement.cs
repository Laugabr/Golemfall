using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class CharacterMovement : NetworkBehaviour
{
    [SerializeField] private SimpleKCC kcc; //kcc: kinematic character controller
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpPower = 10f;

    [SerializeField] private Transform camTarget; //Cam Holder 

    [Networked] private NetworkButtons PreviousButtons { get; set; }
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private LayerMask groundLayer;
    private float nextAttackTime;
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    private bool canDash = true;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float lastDashTime = 0;
    public override void Spawned()
    {
        kcc.SetGravity(Physics.gravity.y * 2f);

        if (HasInputAuthority)
        {
            //            CameraFollow.Instance.SetTarget(camTarget);
        }
    }

    public override void FixedUpdateNetwork() //executing logic that affects gameplay
    {
        if (GetInput(out NetInputPlayer input)) //gets the input of each client
        {

            if (Input.GetMouseButtonDown(0))
            {
                HandleAttack();
            }

            Vector3 worldDirection = kcc.TransformRotation * new Vector3(input.Direction.x, 0f, input.Direction.y); //take the kcc transform rotation and we multiply it by the direction of the input
            float jump = 0f;

            if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && kcc.IsGrounded) //check if player pressed jump button and is grounded
            {
                jump = jumpPower;
            }

            lastDashTime -= Time.deltaTime;

            if (Input.GetKey(KeyCode.LeftShift) && !isDashing)
            {
                dashTimer = dashDuration;
                Debug.Log("Start dash");

                lastDashTime = dashCooldown;

                while (dashTimer >= 0)
                {
                    lastDashTime = dashCooldown;

                    canDash = false;
                    Debug.Log("Is dash");

                    kcc.Move(worldDirection * dashSpeed);

                    dashTimer -= Time.deltaTime;

                    if (dashTimer <= 0f)
                    {
                        isDashing = false;
                    }
                    return;

                }



            }

            kcc.Move(worldDirection.normalized * speed, jump); //normalizing the wD vector to prevent cheating
            PreviousButtons = input.Buttons;
        }
    }


    public override void Render()
    {
        UpdateCamTarget();
    }

    private void UpdateCamTarget()
    {
        camTarget.localPosition = transform.position;
    }
    void HandleAttack()
    {
        if (Time.time >= nextAttackTime)
        {
        nextAttackTime = Time.time + attackCooldown;

        // Hacemos un raycast desde la cámara al punto clickeado
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Vector3 targetPos = hit.point;
            Vector3 direction = (targetPos - transform.position);
            direction.y = 0f;
            direction.Normalize();

            // Girar al personaje hacia el punto
            transform.forward = direction;

                    // Lanzar proyectil (si tenés uno)
            GameObject proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(direction));

            // 🚀 importante: asignar el dueño
            proj.GetComponentInChildren<Proyectil>().SetOwner(gameObject);

            Rigidbody rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = direction * projectileSpeed;

                // También podés agregar animación o sonido de ataque acá
            }
        }
    }
}
