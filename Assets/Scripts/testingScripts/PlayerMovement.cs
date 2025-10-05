using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform; // referencia a la cámara

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

        // dirección basada en la cámara
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // quitamos la inclinación vertical (si la cámara mira hacia arriba o abajo)
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // movimiento final según entrada + orientación de la cámara
        Vector3 move = forward * inputZ + right * inputX;

        controller.Move(move * moveSpeed * Time.deltaTime);

        // si se está moviendo, rotar el jugador hacia esa dirección
        if (move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
