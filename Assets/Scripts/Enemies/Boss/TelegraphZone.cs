using UnityEngine;
using Fusion;

public class TelegraphZone : NetworkBehaviour
{
    private float duration;
    private System.Action onComplete;

    public void Init(float duration, System.Action callback)
    {
        this.duration = duration;
        this.onComplete = callback;

        Debug.Log("[Telegraph] Activado");

        Invoke(nameof(Execute), duration);
    }

    void Execute()
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log("[Telegraph] Ejecutando ataque");

        onComplete?.Invoke();

        Runner.Despawn(Object);
    }
}