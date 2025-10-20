using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessageManager : MonoBehaviour
{
    public static MessageManager Instance { get; private set; }

    [SerializeField] private GameObject messagePanel; // panel con texto
    [SerializeField] private TextMeshProUGUI messageText;

    [SerializeField] private float showSeconds = 1.4f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (messagePanel != null) messagePanel.SetActive(false);
    }

    public void Show(string txt)
    {
        if (messagePanel == null || messageText == null) return;
        StopAllCoroutines();
        messageText.text = txt;
        messagePanel.SetActive(true);
        StartCoroutine(HideAfter());
    }

    private IEnumerator HideAfter()
    {
        yield return new WaitForSeconds(showSeconds);
        messagePanel.SetActive(false);
    }
}

