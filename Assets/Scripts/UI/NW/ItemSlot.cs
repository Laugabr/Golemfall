using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlot : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text statsText;

    public void SetData(ItemData data)
    {
        // Icono
        icon.sprite = data.icon;
        icon.color = data.color;

        // Nombre
        nameText.text = data.displayName;

        // Stats
        statsText.text = FormatStats(data.stats);
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

