using UnityEngine;
using System;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public GameObject LocalPlayer;

    public event Action OnPlayerAttack;
    public event Action OnPlayerDeath;

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

    // Called from input (example: pressing attack button)
    public void TriggerAttack() => OnPlayerAttack?.Invoke();
    public void InvokePlayerDeath() => OnPlayerDeath?.Invoke();
}
