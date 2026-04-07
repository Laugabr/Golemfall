using System;
using UnityEngine;

public class GameEvents : MonoBehaviour
{
    public static GameEvents instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Evento que se dispara cuando se hace logout
    public event Action OnLogout;

    public void TriggerLogout()
    {
        OnLogout?.Invoke();
    }
}