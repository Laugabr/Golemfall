using UnityEngine;

public class DashTrailController : MonoBehaviour
{
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private NetCharacterController controller;

    private bool _wasDashing;

    private void Awake()
    {
        if (trail == null) trail = GetComponent<TrailRenderer>();
        if (controller == null) controller = GetComponentInParent<NetCharacterController>();

        if (trail != null) trail.emitting = false;
    }

    private void Update()
    {
        if (controller == null || trail == null) return;

        bool isDashing = controller.IsDashing;

        if (isDashing && !_wasDashing)
            trail.emitting = true;
        else if (!isDashing && _wasDashing)
            trail.emitting = false;

        _wasDashing = isDashing;
    }
}