using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class HitFeedback : MonoBehaviour
{
    [SerializeField] private Color hitColor = Color.black;
    [SerializeField] private float duration = 0.03f;

    private Renderer rend;
    private Color originalColor;
    private Coroutine currentCoroutine;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
    }

    public void FlashHit()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        rend.material.color = hitColor;
        yield return new WaitForSeconds(duration);
        rend.material.color = originalColor;
    }
}
