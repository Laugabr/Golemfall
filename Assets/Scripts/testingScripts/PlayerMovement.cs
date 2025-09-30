using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Salto")]
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Física")]
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        float inputX = Input.GetAxis("Horizontal"); // A/D o ←/→
        float inputZ = Input.GetAxis("Vertical");   // W/S o ↑/↓

        Vector3 move = transform.right * inputX + transform.forward * inputZ;
        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    private void HandleJump()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // mantiene pegado al suelo
        }

        if (Input.GetButtonDown("Jump") && isGrounded) // "Jump" = barra espaciadora
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
