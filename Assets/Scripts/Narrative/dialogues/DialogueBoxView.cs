using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vista del diálogo. Se suscribe a los eventos de DialogueController y muestra
/// el panel con el nombre del hablante, retrato (si hay) y el texto de la línea actual.
/// Animación: fade in / fade out vía CanvasGroup. El padre del panel queda siempre activo.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DialogueBoxView : MonoBehaviour
{
    [Header("Panel (hijo que se muestra/oculta)")]
    [SerializeField] private GameObject panel;

    [Header("Refs internas")]
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private GameObject portraitSlot;
    [SerializeField] private Image portraitImage;

    [Header("Tuning")]
    [SerializeField, Min(0f)] private float fadeIn = 0.15f;
    [SerializeField, Min(0f)] private float fadeOut = 0.15f;

    private CanvasGroup cg;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        // Escape mientras hay diálogo activo: hace lo mismo que F (avanzar de línea
        // o cerrar al terminar), pero NUNCA abre. Lector único: este componente es
        // una sola instancia, así que un Escape => un Advance(). Gateamos por
        // IsInDialogue (no por panel.activeSelf) porque al terminar el diálogo el
        // State pasa a Idle ANTES del fade-out, evitando avanzar durante el cierre.
        if (Input.GetKeyDown(KeyCode.Escape)
            && DialogueController.Instance != null
            && DialogueController.Instance.IsInDialogue)
        {
            DialogueController.Instance.Advance();
        }
    }

    private void OnEnable()
    {
        if (DialogueController.Instance != null)
            Subscribe(DialogueController.Instance);
    }

    private void Start()
    {
        // Por si el controller no estaba listo en OnEnable (orden de inicialización)
        if (DialogueController.Instance != null)
            Subscribe(DialogueController.Instance);
    }

    private void OnDisable()
    {
        if (DialogueController.Instance != null)
            Unsubscribe(DialogueController.Instance);
    }

    private bool subscribed;

    private void Subscribe(DialogueController c)
    {
        if (subscribed) return;
        c.OnDialogueStarted += HandleStarted;
        c.OnLineChanged     += HandleLineChanged;
        c.OnDialogueEnded   += HandleEnded;
        subscribed = true;
    }

    private void Unsubscribe(DialogueController c)
    {
        if (!subscribed) return;
        c.OnDialogueStarted -= HandleStarted;
        c.OnLineChanged     -= HandleLineChanged;
        c.OnDialogueEnded   -= HandleEnded;
        subscribed = false;
    }

    private void HandleStarted(NpcData npc, DialogueLine line)
    {
        if (panel != null) panel.SetActive(true);
        Bind(npc, line);
        StartFade(1f, fadeIn);
    }

    private void HandleLineChanged(DialogueLine line)
    {
        Bind(_currentNpc, line);
    }

    private void HandleEnded()
    {
        StartFade(0f, fadeOut, hidePanelOnEnd: true);
    }

    // Cacheamos el NPC actual para no romper el render entre líneas
    private NpcData _currentNpc;

    private void Bind(NpcData npc, DialogueLine line)
    {
        _currentNpc = npc;

        // Nombre del hablante: override si está, si no el del NPC
        string speaker = !string.IsNullOrEmpty(line.speakerNameOverride)
            ? line.speakerNameOverride
            : (npc != null ? npc.displayName : "");
        if (speakerNameText != null) speakerNameText.text = speaker;

        // Texto
        if (lineText != null) lineText.text = line.text;

        // Retrato: el de la línea pisa al del NPC. Si ninguno, ocultar el slot.
        Sprite portrait = line.portrait != null
            ? line.portrait
            : (npc != null ? npc.portrait : null);

        bool hasPortrait = portrait != null;
        if (portraitSlot != null) portraitSlot.SetActive(hasPortrait);
        if (portraitImage != null && hasPortrait) portraitImage.sprite = portrait;
    }

    private void StartFade(float to, float duration, bool hidePanelOnEnd = false)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeTo(to, duration, hidePanelOnEnd));
    }

    private IEnumerator FadeTo(float to, float duration, bool hidePanelOnEnd)
    {
        float from = cg.alpha;

        if (duration <= 0f)
        {
            cg.alpha = to;
        }
        else
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            cg.alpha = to;
        }

        if (hidePanelOnEnd && panel != null) panel.SetActive(false);
        fadeRoutine = null;
    }
}