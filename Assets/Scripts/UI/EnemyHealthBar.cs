using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Transform billboard; // el canvas root
    
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        // Siempre mira a la cámara
        if (mainCam != null && billboard != null)
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