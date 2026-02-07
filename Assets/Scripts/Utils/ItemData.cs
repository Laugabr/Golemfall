using UnityEngine;

/*
  ItemData
 
  ScriptableObject representing an item definition.
  Stores metadata (ID, name, icon), visuals, prefab reference,
  equipability flags, and any stat modifiers the item provides.
 */

[CreateAssetMenu(menuName = "Prototype/Item")]
public class ItemData : ScriptableObject
{
    public string id;  // Unique item identifier
    public string displayName; // Name shown in UI
    public Sprite icon; // Inventory icon
    public Color color = Color.white; // Optional color tint for UI
    public GameObject worldPrefab; // Prefab instantiated when item is dropped in the world
    public bool isEquipable = true; // Whether the item can be equipped
    public Stats stats;  // Stat modifiers provided by the item

    public static ItemData GetItem(short key) => ResourcesManager.instance.inventoryItemBank.GetValue<ItemData>(key); //Recuperamos el item mediante el uso de la key
    public static short GetKey(ItemData item) => ResourcesManager.instance.inventoryItemBank.GetKey(item); //Recuperamos la key mediante el uso del item
}
