using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vista de notificación. Un solo prefab con dos layouts hijos (Toast y Banner).
/// Activa el layout correcto según la categoría y rellena texto/ícono.
/// Animación: fade in / hold / fade out vía CanvasGroup.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class NotificationView : MonoBehaviour
{
    [Header("Layouts (un solo hijo activo a la vez)")]
    [SerializeField] private GameObject toastLayout;
    [SerializeField] private GameObject bannerLayout;

    [Header("Toast — refs internas")]
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private Image toastIcon;
    [SerializeField] private GameObject toastIconSlot;

    [Header("Banner — refs internas")]
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private Image bannerIcon;
    [SerializeField] private GameObject bannerIconSlot;

    [Header("Tuning")]
    [SerializeField, Min(0f)] private float fadeIn = 0.2f;
    [SerializeField, Min(0f)] private float fadeOut = 0.3f;

    private CanvasGroup cg;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    public virtual IEnumerator Play(NotificationRequest req)
    {
        ApplyLayout(req);

        // Fade in
        yield return Fade(0f, 1f, fadeIn);

        // Hold: duración total menos los fades
        float hold = Mathf.Max(0f, req.duration - fadeIn - fadeOut);
        if (hold > 0f) yield return new WaitForSeconds(hold);

        // Fade out
        yield return Fade(cg.alpha, 0f, fadeOut);
    }

    public virtual IEnumerator FadeOut(float duration)
    {
        yield return Fade(cg.alpha, 0f, duration);
    }

    private void ApplyLayout(NotificationRequest req)
    {
        bool isToast = req.category == NotificationCategory.Toast;

        if (toastLayout != null)  toastLayout.SetActive(isToast);
        if (bannerLayout != null) bannerLayout.SetActive(!isToast);

        if (isToast) Bind(toastText, toastIcon, toastIconSlot, req);
        else         Bind(bannerText, bannerIcon, bannerIconSlot, req);
    }

    private static void Bind(TMP_Text textTarget, Image iconTarget, GameObject iconSlot, NotificationRequest req)
    {
        if (textTarget != null) textTarget.text = req.text;

        bool hasIcon = req.icon != null;
        if (iconSlot != null) iconSlot.SetActive(hasIcon);
        if (iconTarget != null && hasIcon) iconTarget.sprite = req.icon;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            cg.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // independiente de Time.timeScale
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}