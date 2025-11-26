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
            result += $"{s.statType}: +{s.statValue}\n";
        }

        return result.TrimEnd('\n');
    }
}

