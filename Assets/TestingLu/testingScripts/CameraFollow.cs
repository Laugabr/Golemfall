using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -6f);
    [SerializeField] private float smoothTime = 0.3f; // más alto = más delay
    [SerializeField] private float rotationSpeed = 3f;

    private Vector3 currentVelocity;
    private float yaw;
    private float pitch;

    private void LateUpdate()
    {
        if (target == null) return;

        // Rotación con el mouse
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -35f, 60f);

        // Calcular posición deseada
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = target.position + rotation * offset;

        // Movimiento con suavizado natural
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);

        // Que mire al jugador
        transform.LookAt(target);
    }
}
