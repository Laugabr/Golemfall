using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Represents a UI slot for displaying an item and its stats
public class ItemSlot : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text statsText;

    private ItemData itemData;

    // Set the UI elements based on the given ItemData
    public void SetData(ItemData data)
    {
        itemData = data;

        // Icono
        icon.sprite = data.icon;
        icon.color = data.color;

        // Nombre
        nameText.text = data.displayName;

        // Stats
        statsText.text = FormatStats(data.stats);
    }

    // Returns the stored ItemData
    public ItemData GetItemData()
    {
        return itemData;
    }

    // Returns the item's ID, or empty string if none
    public string GetItemID()
    {
        return itemData != null ? itemData.id : "";
    }

    // Format stats into a readable string for the UI
    private string FormatStats(Stats stats)
    {
        if (stats == null || stats.statInfo.Count == 0)
            return "";

        string result = "";

        foreach (var s in stats.statInfo)
        {
            result += $"{s.statType}: {s.statValue:+#;-#;0}\n";
        }

        return result.TrimEnd('\n');
    }

    // Single source of truth para colocar un item UI dentro de un container.
    // Lo usan ItemContainerSlot.AssignItem y todos los IDropHandler
    // (CraftingDropSlot, EquipSlotDrop, CollectedSlotDrop) para garantizar
    // que el item quede siempre con la misma posición y escala canónicas,
    // sin importar por cuál camino llegue al container.
    public static void PlaceInto(Transform item, Transform target)
    {
        item.SetParent(target, false);
        if (item is RectTransform rt)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}