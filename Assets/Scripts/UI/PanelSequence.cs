using UnityEngine;
using System.Collections;

public class PanelSequence : MonoBehaviour
{
    public CanvasGroup panel1;
    public CanvasGroup panel2;
    public CanvasGroup panel3;

    public float fadeDuration = 1f; // duración del fundido
    public float displayDuration = 3f; // tiempo visible entre fundidos

    void Start()
    {
        // Al iniciar, comenzar la secuencia
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // Asegurar que los paneles empiecen invisibles excepto el primero
        SetPanelVisible(panel1, true);
        SetPanelVisible(panel2, false);
        SetPanelVisible(panel3, false);

        // Panel 1 → 2
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(FadePanels(panel1, panel2));

        // Panel 2 → 3
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(FadePanels(panel2, panel3));

        // Panel 3 queda visible
    }

    IEnumerator FadePanels(CanvasGroup from, CanvasGroup to)
    {
        float t = 0f;
        SetPanelVisible(to, true);

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float normalized = t / fadeDuration;

            from.alpha = Mathf.Lerp(1f, 0f, normalized);
            to.alpha = Mathf.Lerp(0f, 1f, normalized);

            yield return null;
        }

        from.alpha = 0f;
        SetPanelVisible(from, false);
        to.alpha = 1f;
    }

    void SetPanelVisible(CanvasGroup panel, bool visible)
    {
        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }
}
