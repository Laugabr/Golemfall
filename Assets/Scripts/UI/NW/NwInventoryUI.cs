using System.Collections.Generic;
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
            targetInventory.IsDirty = false;
            Refresh();
        }
    }

    public void TogglePanel()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }

    public void Refresh()
    {
        if (targetInventory == null) return;

        // Limpiar slots de inventario
        foreach (var s in inventorySlots)
            s.ClearSlot();

        // Limpiar slots de equip
        foreach (var s in equipSlots)
            s.ClearSlot();

        // Llenar inventario
        int i = 0;
        foreach (var slot in targetInventory.LocalItems)
        {
            if (i >= inventorySlots.Length) break;
            ItemData data = ItemData.GetItem(slot.itemKey);
            if (data == null) { i++; continue; }

            bool isEquipped = targetInventory.EquippedItems.Contains(slot.itemKey);
            if (!isEquipped)
            {
                AddItemToSlot(inventorySlots[i], data);
                i++;
            }
        }

        // Llenar equip slots
        int e = 0;
        foreach (var key in targetInventory.EquippedItems)
        {
            if (e >= equipSlots.Length) break;
            ItemData data = ItemData.GetItem(key);
            if (data != null)
                AddItemToSlot(equipSlots[e], data);
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
