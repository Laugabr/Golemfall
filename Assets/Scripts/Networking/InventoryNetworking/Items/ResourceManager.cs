using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    #region Singleton

    public static ResourcesManager instance;

    private void Awake()
    {
        if (instance)
        {
            Debug.LogWarning("Instance already exists!");
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    #endregion


    [Header("------ Items -----")]
    public ResourceBank inventoryItemBank;


}
