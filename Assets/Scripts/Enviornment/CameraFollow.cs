    using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance => _instance;
    private static CameraFollow _instance;

    private Transform targetTransform;
    void Awake()
    {
        if (_instance = null)
        {
            _instance = this;
            DontDestroyOnLoad(_instance);
        }
        else
        {
            Destroy(this);
        }
    }

    void Update()
    {
        if (targetTransform != null)
        {
            transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        targetTransform = newTarget;

    }

}
