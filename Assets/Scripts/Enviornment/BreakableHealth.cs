    using UnityEngine;

public class BreakableHealth : Health
{
    [SerializeField] private BreakableObject _breakableObject;

    void Awake()
    {
        _breakableObject = GetComponent<BreakableObject>();

        if(_breakableObject == null)
        {
            Debug.Log( gameObject.name  + "Breakable drop component not found");
        }
    }

    public override void Die()
    {
        _breakableObject.Break();
    }
}
