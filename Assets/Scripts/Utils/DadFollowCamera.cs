using UnityEngine;

public class DadFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponentInParent<Transform>();
        }
    }

    private void Update()
    {
        if (target != null)
        {
            target.position = transform.position;
        }
    }
}
