using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles enemy references, spawn control, and central enemy utilities.
/// </summary>
public class EnemiesManager : MonoBehaviour
{
    public static EnemiesManager Instance { get; private set; }

    private List<GameObject> activeEnemies = new List<GameObject>();  //Test

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

    // TODO: Implement enemy spawning and tracking
}