using UnityEngine;

public class UIRoot : MonoBehaviour
{
    public static UIRoot Instance;
    public Transform dragLayer;

    private void Awake()
    {
        Instance = this;
    }
}

