using UnityEngine;

public class UIRoot : MonoBehaviour
{
    public static UIRoot Instance;

    public Transform dragLayer; // un panel vacio por encima de todo

    private void Awake()
    {
        Instance = this;
    }
}

