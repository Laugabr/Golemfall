using UnityEngine;
using System.Collections;
using Fusion.Addons.SimpleKCC;

public class PlayerDashKinematic : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 5f;        // Cuánto se desplaza el dash
    [SerializeField] private float dashDuration = 0.2f;      // Cuánto dura el dash
    [SerializeField] private float dashCooldown = 1f;        // Tiempo entre dashes
    [SerializeField] private AnimationCurve dashSpeedCurve;  // Curva para suavizar
    [SerializeField] private SimpleKCC kcc;  // Curva para suavizar

    private bool isDashing = false;
    private bool canDash = true;

    private void Awake()
    {
        if (dashSpeedCurve == null)
            dashSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(PerformDash());
        }
    }

    private IEnumerator PerformDash()
    {
        canDash = false;
        isDashing = true;

        // Dirección del dash
        Vector3 dashDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (dashDirection.sqrMagnitude < 0.1f)
        {
            dashDirection = transform.forward; // Si no hay input, dash hacia adelante
        }
        dashDirection.Normalize();

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + dashDirection * dashDistance;

        float elapsed = 0f;
        kcc.enabled = false;

        while (elapsed < dashDuration)
        {

        float t = Mathf.Clamp01(elapsed / dashDuration);
            float speedMultiplier = dashSpeedCurve.Evaluate(t);

        transform.position = Vector3.Lerp(startPos, endPos, dashSpeedCurve.Evaluate(t));    

            elapsed += Time.deltaTime;
            yield return null;
        }
        kcc.enabled = true;

        isDashing = false;

        // Cooldown
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}