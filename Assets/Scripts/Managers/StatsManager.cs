using UnityEngine;

/// <summary>
/// Handles stat formulas and calculations for any entity (player or enemy).
/// </summary>
public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

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

    

}
