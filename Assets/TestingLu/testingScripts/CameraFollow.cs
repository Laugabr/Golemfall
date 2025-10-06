using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -6f);
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float rotationSpeed = 3f;

    private float yaw;   // rotación horizontal
    private float pitch; // rotación vertical

    private void LateUpdate()
    {
        if (target == null) return;

        // Leer movimiento del mouse
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -35f, 60f); // limitar vertical

        // Rotar offset alrededor del jugador según yaw/pitch
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = target.position + rotation * offset;

        // Movimiento suavizado
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Que mire al jugador
        transform.LookAt(target);
    }
}
