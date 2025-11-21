using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlot : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text statsText;

    private ItemData itemData; 
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

    public ItemData GetItemData()
    {
        return itemData;
    }

    public string GetItemID()
    {
        return itemData != null ? itemData.id : "";
    }


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

