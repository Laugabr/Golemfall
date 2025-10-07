using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Item")]
public class ItemData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public Color color = Color.white;
    public GameObject worldPrefab; // prefab que se instanciará al arrojar
    public bool isEquipable = true;

    // Para prototipo simple: multipliers para velocidad y salto
    [Tooltip("Multiplicador aplicado a moveSpeed (1 = sin cambio)")]
    public float speedMultiplier = 1f;
    [Tooltip("Multiplicador aplicado a jumpHeight (1 = sin cambio)")]
    public float jumpMultiplier = 1f;
}
