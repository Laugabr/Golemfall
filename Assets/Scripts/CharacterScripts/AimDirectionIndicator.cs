using UnityEngine;
using Fusion;


public class AimDirectionIndicator : NetworkBehaviour
{
    [SerializeField] private Transform indicatorSprite;
    [SerializeField] private float heightOffset = 0.05f;

    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
    }

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
        {
            indicatorSprite.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (indicatorSprite == null || _cam == null) return;

        Plane plane = new Plane(Vector3.up, transform.position);
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);

        if (plane.Raycast(ray, out float dist))
        {
            Vector3 hitPoint = ray.GetPoint(dist);
            Vector3 dir = hitPoint - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                indicatorSprite.position = transform.position + Vector3.up * heightOffset;
                indicatorSprite.rotation = Quaternion.Euler(90f, angle, 0f);
            }
        }
    }
}