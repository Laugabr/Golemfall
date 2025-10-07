using UnityEngine;
using UnityEngine.UI;

public class InteractPrompt : MonoBehaviour
{
    public static InteractPrompt Instance { get; private set; }

    [SerializeField] private GameObject promptGO; // pequeño panel con texto "F"
    [SerializeField] private Text promptText;
    private RectTransform rect;
    private Camera mainCam;
    private Transform tracked; // transform del item que estamos marcando

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        rect = promptGO?.GetComponent<RectTransform>();
        if (promptGO != null) promptGO.SetActive(false);
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (tracked != null && rect != null)
        {
            Vector3 screen = mainCam.WorldToScreenPoint(tracked.position + Vector3.up * 0.6f);
            rect.position = screen;
        }
    }

    public void Show(Transform t, string text = "F")
    {
        if (promptGO == null) return;
        tracked = t;
        promptText.text = text;
        promptGO.SetActive(true);
    }

    public void Hide()
    {
        tracked = null;
        if (promptGO != null) promptGO.SetActive(false);
    }
}

