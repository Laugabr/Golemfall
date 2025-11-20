using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;

    private ItemData currentItem;

    public void Setup(ItemData item)
    {
        currentItem = item;

        icon.sprite = item.icon;
        nameText.text = item.displayName;
    }
}

