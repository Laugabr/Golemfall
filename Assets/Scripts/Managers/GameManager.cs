using UnityEngine;
using Fusion;

/// <summary>
/// Local/offline GameManager singleton.
/// Always present in scene. Coordinates local systems like UI, Audio, Input and references to gameplay managers.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }



    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {

    }
}
