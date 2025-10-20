using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform;

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
        Vector3 move = GetMovementInput(); // solo calcula movimiento
        HandleJump();
        ApplyGravity();

        // mover todo junto (horizontal + vertical)
        controller.Move((move * moveSpeed + velocity) * Time.deltaTime);

        Debug.DrawRay(transform.position, Vector3.down * (controller.height / 2 + 0.2f), Color.red);
    }

    private Vector3 GetMovementInput()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * inputZ + right * inputX;

        if (move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        return move;
    }


    private bool CheckGrounded()
    {
        float rayLength = controller.height / 2 + 0.2f;
        return Physics.Raycast(transform.position, Vector3.down, rayLength);
    }

    private void HandleJump()
    {
        isGrounded = CheckGrounded();

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = 0f; // reset vertical velocity before jump
            velocity.y += Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
