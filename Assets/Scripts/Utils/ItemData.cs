using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Item")]
public class ItemData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public Color color = Color.white;
    public GameObject worldPrefab;
    public bool isEquipable = true;

    public Stats stats; // 👈 Nuevo campo
}
