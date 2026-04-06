using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class NwInventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemContainerSlot[] inventorySlots;
    [SerializeField] private ItemContainerSlot[] equipSlots;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject inventoryPanel;

    private NetworkInventory targetInventory;
    private NetworkRunner runner;
    private List<short> visualOrder = new List<short>();
    private bool suppressRefresh = false;

    private void Start()
    {
        runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("[NwInventoryUI] No se encontró NetworkRunner.");
            return;
        }
        InvokeRepeating(nameof(TryBindToLocalPlayer), 0.5f, 0.5f);
    }

    private void TryBindToLocalPlayer()
    {
        if (targetInventory != null)
        {
            CancelInvoke(nameof(TryBindToLocalPlayer));
            return;
        }

        var allStats = FindObjectsOfType<PlayerStats>();
        foreach (var ps in allStats)
        {
            if (ps.Object == null || !ps.Object.IsValid) continue;
            if (ps.Object.InputAuthority != runner.LocalPlayer) continue;

            targetInventory = ps.GetComponent<NetworkInventory>();
            if (targetInventory != null)
            {
                Debug.Log("[NwInventoryUI] Vinculado al inventario local.");
                CancelInvoke(nameof(TryBindToLocalPlayer));
                Refresh();
            }
            return;
        }
    }

    private void Update()
    {
        if (targetInventory == null) return;
        if (!targetInventory.Object.IsValid) return;

        if (targetInventory.IsDirty)
        {
            if (ItemSlotDrag.IsDragging) return;

            if (suppressRefresh)
            {
                suppressRefresh = false;
                targetInventory.IsDirty = false;
                return;
            }

            targetInventory.IsDirty = false;
            Refresh();
        }
    }

    public void SuppressNextRefresh()
    {
        suppressRefresh = true;
    }

    public void TogglePanel()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }

    public void Refresh()
    {
        if (targetInventory == null) return;

        var currentKeys = targetInventory.LocalItems
            .Select(s => s.itemKey)
            .Where(k => !targetInventory.EquippedItems.Contains(k))
            .ToList();

        foreach (var key in currentKeys)
            if (!visualOrder.Contains(key))
                visualOrder.Add(key);

        visualOrder.RemoveAll(k => !currentKeys.Contains(k));

        foreach (var s in inventorySlots) s.ClearSlot();
        foreach (var s in equipSlots) s.ClearSlot();

        for (int i = 0; i < visualOrder.Count && i < inventorySlots.Length; i++)
        {
            ItemData data = ItemData.GetItem(visualOrder[i]);
            if (data != null) AddItemToSlot(inventorySlots[i], data);
        }

        int e = 0;
        foreach (var key in targetInventory.EquippedItems)
        {
            if (e >= equipSlots.Length) break;
            ItemData data = ItemData.GetItem(key);
            if (data != null) AddItemToSlot(equipSlots[e], data);
            e++;
        }
    }

    private void AddItemToSlot(ItemContainerSlot container, ItemData data)
    {
        if (container == null || !container.IsEmpty) return;

        GameObject go = Instantiate(slotPrefab);
        var itemSlot = go.GetComponent<ItemSlot>();
        if (itemSlot == null)
        {
            Destroy(go);
            return;
        }
        itemSlot.SetData(data);
        container.AssignItem(itemSlot);
    }

    public NetworkInventory GetTargetInventory() => targetInventory;
}