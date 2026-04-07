using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    #region Singleton

    public static ResourcesManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Instance already exists! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Suscribirse al evento de logout
        if (GameEvents.instance != null)
        {
            GameEvents.instance.OnLogout += HandleLogout;
        }
    }

    private void HandleLogout()
    {
        // Limpiarse a sí mismo cuando hay logout
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        if (GameEvents.instance != null)
        {
            GameEvents.instance.OnLogout -= HandleLogout;
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    #endregion

    [Header("------ Items -----")]
    public ResourceBank inventoryItemBank;
}