using UnityEngine;
using Fusion;

// Agregá este componente a cualquier DestructibleObject que deba liberar un collider al romperse.
// Funciona suscribiéndose al evento que DestructibleObject dispara antes de despawnearse.
public class OnDestroyUnlockCollider : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToDisable; // colliders, puertas, barreras, etc.

    private void Awake()
    {
        // Busca el DestructibleObject en el mismo GO y se suscribe
        var destructible = GetComponent<DestructibleObject>();
        if (destructible != null)
            destructible.OnDestroyed += HandleDestroyed;
    }

    private void HandleDestroyed()
    {
        foreach (var obj in objectsToDisable)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}