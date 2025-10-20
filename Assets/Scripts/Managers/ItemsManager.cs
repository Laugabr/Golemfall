using NUnit.Framework.Interfaces;
using UnityEngine;

/// <summary>
/// Manages item definitions, lookups, and item-related utility functions.
/// </summary>
public class ItemsManager : MonoBehaviour
{
    public static ItemsManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    // TODO: Implement item database or registry

}
