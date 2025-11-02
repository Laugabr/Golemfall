using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PanelInfo
{
    public CanvasGroup panel;
    public float fadeDuration = 1.5f;
    public float displayDuration = 4f;
}

public class PanelSequence : MonoBehaviour
{
    [Header("Panels")]
    public List<PanelInfo> panels;
    public CanvasGroup blackOverlay;

    void Start()
    {
        // Inicializar todos los panels invisibles
        foreach (PanelInfo info in panels)
        {
            SetPanel(info.panel, false);
        }

        if (blackOverlay != null)
            blackOverlay.alpha = 0f;

        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            PanelInfo currentInfo = panels[i];
            CanvasGroup current = currentInfo.panel;

            // Mostrar panel actual
            SetPanel(current, true);

            // Esperar displayDuration
            yield return new WaitForSeconds(currentInfo.displayDuration);

            // Si hay siguiente panel, hacer fundido
            if (i < panels.Count - 1)
            {
                PanelInfo nextInfo = panels[i + 1];
                yield return StartCoroutine(FadeToNextPanel(currentInfo, nextInfo));
            }
        }
    }

    IEnumerator FadeToNextPanel(PanelInfo fromInfo, PanelInfo toInfo)
    {
        float t = 0f;
        float duration = Mathf.Max(fromInfo.fadeDuration, toInfo.fadeDuration);

        CanvasGroup from = fromInfo.panel;
        CanvasGroup to = toInfo.panel;

        // Activar panel siguiente (invisible) y overlay
        SetPanel(to, true);
        if (blackOverlay != null)
            blackOverlay.alpha = 1f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration;

            // Fundido directo de panels
            from.alpha = Mathf.Lerp(1f, 0f, normalized * (duration / fromInfo.fadeDuration));
            to.alpha = Mathf.Lerp(0f, 1f, normalized * (duration / toInfo.fadeDuration));

            // Overlay cubre cualquier hueco
            if (blackOverlay != null)
                blackOverlay.alpha = Mathf.Lerp(1f, 0f, normalized);

            yield return null;
        }

        // Valores finales
        from.alpha = 0f;
        SetPanel(from, false);
        to.alpha = 1f;

        if (blackOverlay != null)
            blackOverlay.alpha = 0f;
    }

    void SetPanel(CanvasGroup panel, bool visible)
    {
        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }
}
