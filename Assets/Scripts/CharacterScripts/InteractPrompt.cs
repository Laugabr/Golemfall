using UnityEngine;
using TMPro;
using System.Collections;

public class InteractPrompt : MonoBehaviour
{
    public static InteractPrompt Instance { get; private set; }

    [SerializeField] private GameObject promptGO;    
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] private float displayDuration = 1f; // ← CONFIGURABLE

    private RectTransform rect;
    private Camera mainCam;
    private Transform tracked;
    private Coroutine hideCoroutine;

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
        if (tracked != null && rect != null && promptGO.activeSelf)
        {
            Vector3 screen = mainCam.WorldToScreenPoint(tracked.position + Vector3.up * 0.6f);
            rect.position = screen;
        }
    }

    public void Show(Transform t, string text = "F")
    {
        tracked = t;
        promptText.text = text;
        promptGO.SetActive(true);

        // Reiniciar el temporizador si ya estaba mostrando otro prompt
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideAfterSeconds());
    }

    private IEnumerator HideAfterSeconds()
    {
        yield return new WaitForSeconds(displayDuration);
        Hide();
    }

    public void Hide()
    {
        tracked = null;
        promptGO.SetActive(false);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = null;
    }
}

