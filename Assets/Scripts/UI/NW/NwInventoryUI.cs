using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handles the UI representation of a NetworkInventory or local InventoryManager
public class NwInventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkInventory targetInventory;
    [SerializeField] private ItemContainerSlot[] slots;       
    [SerializeField] private GameObject slotPrefab;
    private void Awake()
    {
        // Auto-assign slots if not set manually
        if (slots == null || slots.Length == 0)
            slots = GetComponentsInChildren<ItemContainerSlot>(true);
        Debug.Log($"[NwInventoryUI] Slots found: {slots.Length}");
    }

    private void Start()
    {
        if (InventoryManager.Instance == null)
            Debug.LogError("[NwInventoryUI] InventoryManager.Instance es NULL.");

        if (slotPrefab == null)
            Debug.LogError("[NwInventoryUI] slotPrefab NO asignado.");

        if (targetInventory == null)
            Debug.LogWarning("[NwInventoryUI] targetInventory NO asignado (si estás en modo local usa InventoryManager).");

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded += AddItemToUI;
            Debug.Log("[NwInventoryUI] Suscrito a InventoryManager.OnItemAdded");
        }

        if (InventoryManager.Instance != null)
        {
            for (int i = 0; i < InventoryManager.Instance.Count; i++)
            {
                var it = InventoryManager.Instance.GetItemAt(i);
                if (it != null) AddItemToUI(it);
            }
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAdded -= AddItemToUI;
    }

    private void Update()
    {
        if (targetInventory == null) return;
        if (targetInventory.Object == null) return;
        if (!targetInventory.Object.IsValid) return;

        if (targetInventory.IsDirty)
        {
            Debug.Log("[NwInventoryUI] targetInventory.IsDirty -> RefreshFromNetworkInventory");
            targetInventory.IsDirty = false;
            RefreshFromNetworkInventory();
        }
    }

    // Clears UI and populates from targetInventory.Items
    public void RefreshFromNetworkInventory()
    {
        foreach (var s in slots)
            s.ClearSlot();

        foreach (var itemID in targetInventory.Items)
        {
            ItemData data = Resources.Load<ItemData>("DataSO/StatsData/Itemscrafteados/" + itemID);
            if (data == null) { Debug.LogError("[NwInventoryUI] No encontro ItemData for " + itemID); continue; }
            AddItemToUI(data);
        }
    }

    // Core method: adds item to first empty slot
    public void AddItemToUI(ItemData item)
    {
        if (item == null) { Debug.LogWarning("[NwInventoryUI] AddItemToUI recibido NULL"); return; }

        foreach (var s in slots)
        {
            if (s == null) continue;
            if (s.IsEmpty)
            {
                // Instantiate ItemSlot prefab and assign it to the container
                GameObject go = Instantiate(slotPrefab);
                var itemSlot = go.GetComponent<ItemSlot>();
                if (itemSlot == null)
                {
                    Debug.LogError("[NwInventoryUI] slotPrefab NO tiene ItemSlot componente.");
                    Destroy(go);
                    return;
                }

                itemSlot.SetData(item);
                s.AssignItem(itemSlot);
                Debug.Log($"[NwInventoryUI] Item '{item.displayName}' asignado al slot {s.name}");
                return;
            }
        }

        Debug.LogWarning("[NwInventoryUI] No hay slots libres para: " + item.displayName);
    }
}

