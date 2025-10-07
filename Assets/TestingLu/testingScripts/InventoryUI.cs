using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject inventoryPanel; // panel mayor (por defecto desactivado como dijiste)
    [Header("Slots UI (5)")]
    public Image[] slotIcons; // arrastrar 5 Image en inspector
    public Sprite emptySprite;
    [Header("Equip slots UI (2)")]
    public Image[] equipSlotIcons; // 2 images para equipados

    void OnEnable()
    {
        InventoryManager.Instance.OnInventoryChanged += Refresh;
        if (EquipManager.Instance != null) EquipManager.Instance.OnEquipChanged += RefreshEquip;
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnInventoryChanged -= Refresh;
        if (EquipManager.Instance != null) EquipManager.Instance.OnEquipChanged -= RefreshEquip;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }
    }

    public void Refresh()
    {
        // actualizar casilleros recolectados
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (i < InventoryManager.Instance.Count)
            {
                var it = InventoryManager.Instance.GetItemAt(i);
                slotIcons[i].sprite = it != null ? it.icon : emptySprite;
                slotIcons[i].color = it != null ? Color.white : new Color(1,1,1,0.2f);
            }
            else
            {
                slotIcons[i].sprite = emptySprite;
                slotIcons[i].color = new Color(1,1,1,0.2f);
            }
        }
        RefreshEquip();
    }

    public void RefreshEquip()
    {
        if (EquipManager.Instance == null) return;
        for (int i = 0; i < equipSlotIcons.Length; i++)
        {
            if (i < EquipManager.Instance.equipped.Length && EquipManager.Instance.equipped[i] != null)
            {
                var it = EquipManager.Instance.equipped[i];
                equipSlotIcons[i].sprite = it.icon;
                equipSlotIcons[i].color = Color.white;
            }
            else
            {
                equipSlotIcons[i].sprite = emptySprite;
                equipSlotIcons[i].color = new Color(1,1,1,0.2f);
            }
        }
    }
}

