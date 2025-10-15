using UnityEngine;
using System;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public GameObject LocalPlayer;

    // --- Player Events --- (Example)
    /* public event Action OnPlayerAttack;
     public event Action OnPlayerDeath;
     public event Action OnPlayerHit;
    */
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

    public void SetLocalPlayer(GameObject player) => LocalPlayer = player;

    // --- Event Invokers (these would be called by gameplay logic) ---

}