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

    // Runtime-only: se vincula automáticamente en TryBindToLocalPlayer.
    // No se expone al Inspector porque en multiplayer el player local
    // se instancia dinámicamente y no puede asignarse en edit-time.
    private CraftingSystem targetCraftingSystem;

    // Cache del CraftingUI (mismo GameObject). Usado por Refresh para excluir
    // del inventario las keys que están ocupando un slot de craft.
    private CraftingUI craftingUI;

    private int lastCount;

    private NetworkInventory targetInventory;
    private NetworkRunner runner;
    private List<short> visualOrder = new List<short>();
    private bool suppressRefresh = false;

    private void Start()
    {
        craftingUI = GetComponent<CraftingUI>();

        runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("[NwInventoryUI] No se encontró NetworkRunner.");
            return;
        }
        InvokeRepeating(nameof(TryBindToLocalPlayer), 0.5f, 0.5f);
    }

    public void RequestCraft(short a, short b)
    {
        if (targetCraftingSystem == null)
        {
            Debug.LogError("No hay CraftingSystem");
            return;
        }

        targetCraftingSystem.RPC_RequestCraft(a, b);

        // Evita refresh visual raro mientras arrastrás
        SuppressNextRefresh();
    }
    private void TryBindToLocalPlayer()
    {
        if (targetInventory != null)
        {
            CancelInvoke(nameof(TryBindToLocalPlayer));
            return;
        }

        var allStats = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        foreach (var ps in allStats)
        {
            if (ps.Object == null || !ps.Object.IsValid) continue;
            if (ps.Object.InputAuthority != runner.LocalPlayer) continue;

            targetCraftingSystem = ps.GetComponent<CraftingSystem>();
            if (targetCraftingSystem != null)
            {
                //Debug.Log("[CraftingSystem] Vinculado al inventario local.");

                if (craftingUI == null) craftingUI = GetComponent<CraftingUI>();
                if (craftingUI != null) craftingUI.SetCraftingSystem(targetCraftingSystem);

                Refresh();
            }

            targetInventory = ps.GetComponent<NetworkInventory>();
            if (targetInventory != null)
            {
                //Debug.Log("[NwInventoryUI] Vinculado al inventario local.");
                CancelInvoke(nameof(TryBindToLocalPlayer));
                Refresh();
            }


            
            return;
        }
    }

    public void ForceRefresh()
    {
        suppressRefresh = false;
        Refresh();
    }

    private void Update()
    {
        if (targetInventory == null) return;
        if (!targetInventory.Object.IsValid) return;

            if (targetInventory.IsDirty || targetInventory.LocalItems.Count != lastCount)        
            {

            if (ItemSlotDrag.IsDragging)
            {
                SuppressNextRefresh();
            }
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
    {
        bool nowActive = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(nowActive);

        if (nowActive)
        {
            Refresh(); 
        }
    }
    }

        private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (targetInventory == null) return;

         visualOrder.Clear(); 

        var currentKeys = targetInventory.LocalItems
            .Select(s => s.itemKey)
            .Where(k => !targetInventory.EquippedItems.Contains(k)
                        && (craftingUI == null || !craftingUI.IsKeyInCraft(k)))
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
        lastCount = targetInventory.LocalItems.Count;
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

    // --- Quick-move (doble-click) ---

    // Primer slot de inventario vacío, o null si no hay.
    public ItemContainerSlot GetFirstEmptyInventorySlot()
    {
        if (inventorySlots == null) return null;
        foreach (var s in inventorySlots)
            if (s != null && s.IsEmpty) return s;
        return null;
    }

    // Primer slot de equip vacío, o null si no hay.
    public ItemContainerSlot GetFirstEmptyEquipSlot()
    {
        if (equipSlots == null) return null;
        foreach (var s in equipSlots)
            if (s != null && s.IsEmpty) return s;
        return null;
    }
}