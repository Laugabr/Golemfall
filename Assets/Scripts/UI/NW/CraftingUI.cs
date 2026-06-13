using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    [SerializeField] private CraftingDropSlot slotA;
    [SerializeField] private CraftingDropSlot slotB;

    [Header("Preview")]
    [SerializeField] private Image previewIcon;
    [SerializeField] private TMP_Text previewName;
    [SerializeField] private TMP_Text previewStats;
    [SerializeField] private Button craftButton;

    private NwInventoryUI inventoryUI;
    private CraftingSystem craftingSystem;

    private void Awake()
    {
        if (craftButton != null)
            craftButton.onClick.AddListener(OnCraftButtonClicked);
        ShowPreview(null); // estado inicial: sin preview, botón off
    }

    public void SetCraftingSystem(CraftingSystem targetCraftingSystem)
    {
        inventoryUI = FindFirstObjectByType<NwInventoryUI>();
        craftingSystem = targetCraftingSystem;
    }

    // Llamado por CraftingDropSlot al soltar un material o al sacarlo.
    // Ya NO craftea: solo calcula y muestra el resultado de la receta.
    public void UpdatePreview()
    {
        if (craftingSystem == null || slotA.IsEmpty || slotB.IsEmpty)
        {
            ShowPreview(null);
            return;
        }

        var dataA = slotA.CurrentItem?.GetItemData();
        var dataB = slotB.CurrentItem?.GetItemData();
        if (dataA == null || dataB == null) { ShowPreview(null); return; }

        short resultKey = craftingSystem.PreviewResult(ItemData.GetKey(dataA), ItemData.GetKey(dataB));
        ShowPreview(resultKey >= 0 ? ItemData.GetItem(resultKey) : null);
    }

    // El craft real ahora ocurre acá, al apretar el botón.
    public void OnCraftButtonClicked()
    {
        if (craftingSystem == null || slotA.IsEmpty || slotB.IsEmpty) return;

        var dataA = slotA.CurrentItem?.GetItemData();
        var dataB = slotB.CurrentItem?.GetItemData();
        if (dataA == null || dataB == null) return;

        craftingSystem.RPC_RequestCraft(ItemData.GetKey(dataA), ItemData.GetKey(dataB));

        ClearSlots();
        ShowPreview(null);
        inventoryUI?.ForceRefresh();
    }

    private void ShowPreview(ItemData result)
    {
        bool hasResult = result != null;

        if (previewIcon != null)
        {
            previewIcon.enabled = hasResult;
            if (hasResult)
            {
                previewIcon.sprite = result.icon;
                previewIcon.color = result.color;
            }
        }

        if (previewName != null)
            previewName.text = hasResult ? result.displayName : "";

        if (previewStats != null)
            previewStats.text = hasResult ? FormatStats(result.stats) : "";

        if (craftButton != null)
            craftButton.interactable = hasResult;
    }

    private void ClearSlots()
    {
        slotA.Clear();
        slotB.Clear();
    }
    private static string FormatStats(Stats stats)
    {
        if (stats == null || stats.statInfo.Count == 0) return "";

        string result = "";
        foreach (var s in stats.statInfo)
            result += $"{s.statType}: +{s.statValue}\n";
        return result.TrimEnd('\n');
    }
}