using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class InventoryUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject inventoryPanel;
    public Image[] slotIcons;
    public Sprite emptySprite;
    public Image[] equipSlotIcons;

    void Start()
    {
        // Si el InventoryManager no existía cuando se habilitó el UI, nos suscribimos ahora.
        if (InventoryManager.Instance != null)
        {
            Debug.Log("[InventoryUI] Subscribiéndose desde Start al InventoryManager");
            InventoryManager.Instance.OnInventoryChanged += Refresh;
            Refresh(); // refrescamos de entrada por si ya había ítems
        }

        if (EquipManager.Instance != null)
        {
            Debug.Log("[InventoryUI] Subscribiéndose desde Start al EquipManager");
            EquipManager.Instance.OnEquipChanged += RefreshEquip;
            RefreshEquip();
        }
    }

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            // evitar registrarse doble vez
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
            InventoryManager.Instance.OnInventoryChanged += Refresh;
        }

        if (EquipManager.Instance != null)
        {
            EquipManager.Instance.OnEquipChanged -= RefreshEquip;
            EquipManager.Instance.OnEquipChanged += RefreshEquip;
        }
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
            if (inventoryPanel != null)
            {
                bool isActive = inventoryPanel.activeSelf;
                // Si el inventario está abierto a punto de cerrarse y hay un drag activo
                if (isActive && DragData.sourceSlot != null)
                {
                    DragData.sourceSlot.OnEndDrag(new PointerEventData(EventSystem.current));
                }
                isActive = !inventoryPanel.activeSelf;
                inventoryPanel.SetActive(isActive);
            }
        }
    }

    public void Refresh()
    {
        Debug.Log($"[InventoryUI] Refrescando inventario — Items actuales: {InventoryManager.Instance.Count}");

        // actualizar casilleros recolectados
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (i < InventoryManager.Instance.Count)
            {
                var it = InventoryManager.Instance.GetItemAt(i);
                slotIcons[i].sprite = it != null ? it.icon : emptySprite;
                slotIcons[i].color = it != null ? Color.white : new Color(1, 1, 1, 0.2f);
            }
            else
            {
                slotIcons[i].sprite = emptySprite;
                slotIcons[i].color = new Color(1, 1, 1, 0.2f);
            }
        }
        RefreshEquip();
        Debug.Log("Refrescando inventario. Cantidad: " + InventoryManager.Instance.Count);
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
                equipSlotIcons[i].color = new Color(1, 1, 1, 0.2f);
            }
        }
    }
    public void RefreshEquip(ItemData item, bool equipped)
    {
        // simplemente refresca todo el UI, no hace falta usar los parámetros
        RefreshEquip();
    }

}

