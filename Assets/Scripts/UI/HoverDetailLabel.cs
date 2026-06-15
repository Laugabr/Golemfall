using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Etiqueta de UI que muestra un valor COMPACTO por defecto y un valor DETALLADO al hacer hover.
///
/// Responsabilidad única: detectar el hover y decidir qué texto mostrar.
/// No sabe nada del dato que representa (vida, XP, etc.); quien la usa le pasa
/// las dos cadenas vía Set(compacto, detallado).
///
/// Uso en el editor:
///  - Colgar este componente sobre el GameObject del panel/barra que recibe el mouse
///    (debe tener un Graphic con Raycast Target activado y un EventSystem en la escena).
///  - Asignar en 'label' el TMP_Text donde se escribe.
///
/// Reutilizable para vida, experiencia o cualquier stat futuro.
/// </summary>
public class HoverDetailLabel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text label;

    private string compact = string.Empty;
    private string detailed = string.Empty;
    private bool isHovering;

    /// <summary>
    /// Define qué mostrar en estado normal y en hover. Re-renderiza al instante.
    /// Llamar cada vez que el dato cambie.
    /// </summary>
    public void Set(string compactText, string detailedText)
    {
        compact = compactText;
        detailed = detailedText;
        Render();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        Render();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        Render();
    }

    private void Render()
    {
        if (label != null)
            label.text = isHovering ? detailed : compact;
    }
}