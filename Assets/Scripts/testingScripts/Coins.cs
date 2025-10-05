using UnityEngine;

public class Coins : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private int experienceAmount;

    void OnCollisionEnter(Collision collision)
    {
        BasicEventsManager.OnExperienceGain.Invoke(experienceAmount);

        Destroy(this);
    }
    void Update()
    {
        transform.Rotate(0, 5 * Time.deltaTime, 0, Space.Self);
    }

}
