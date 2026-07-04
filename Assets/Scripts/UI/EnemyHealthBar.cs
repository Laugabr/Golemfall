using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Transform billboard; // el canvas root
    private Renderer enemyRenderer; 
    private float maxBillboardDistance = 30f;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
        Renderer rend = GetComponentInParent<Renderer>();

        if (rend != null)
            enemyRenderer = rend;


        maxBillboardDistance = GetComponentInParent<EnemyAI>().VisionRange * 2;
    }



    private void LateUpdate()
    {
        if (mainCam == null || billboard == null) return;

        // Salta el trabajo si está lejos o fuera de cámara
        float sqrDist = (transform.position - mainCam.transform.position).sqrMagnitude;
        if (sqrDist > maxBillboardDistance * maxBillboardDistance) return;
        if (enemyRenderer != null && !enemyRenderer.isVisible) return;

        billboard.rotation = mainCam.transform.rotation;
    }

    public void SetMaxHealth(int max)
    {
        slider.maxValue = max;
        slider.value = max;
    }

    public void SetHealth(int current)
    {
        slider.value = current;
    }
}