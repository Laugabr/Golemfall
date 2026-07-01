using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reproduce un SFX 2D al hacer click en un Button. Reutilizable: arrastrás el
/// SoundEvent que quieras (click genérico, craftear, etc.) y agregás este
/// componente al Button. Se auto-suscribe al onClick, no hay que cablear nada más.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [SerializeField] private SoundEvent clickSound;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Play);
    }

    private void Play() => clickSound?.Play2D();
}
