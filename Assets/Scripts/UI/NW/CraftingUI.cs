using UnityEngine;

public class CraftingUI : MonoBehaviour
{
    [SerializeField] private CraftingDropSlot slotA;
    [SerializeField] private CraftingDropSlot slotB;

    private NwInventoryUI inventoryUI;
    private CraftingSystem craftingSystem;


    public void SetCraftingSystem(CraftingSystem targetCraftingSystem)
    {
        inventoryUI = FindFirstObjectByType<NwInventoryUI>();
        craftingSystem = targetCraftingSystem;
    }

    public void OnItemPlaced()
    {
        if (slotA.IsEmpty || slotB.IsEmpty)
            return;

        var itemA = slotA.CurrentItem;
        var itemB = slotB.CurrentItem;

        if (itemA == null || itemB == null) return;

        short keyA = ItemData.GetKey(itemA.GetItemData());
        short keyB = ItemData.GetKey(itemB.GetItemData());
        Debug.Log($"[CRAFT UI] itemA name: {itemA.GetItemData()?.name}, keyA: {keyA}");
        Debug.Log($"[CRAFT UI] itemB name: {itemB.GetItemData()?.name}, keyB: {keyB}");
        Debug.Log($"[CRAFT UI] Intentando craftear: keyA={keyA}, keyB={keyB}");
        Debug.Log($"[CRAFT UI] craftingSystem es null? {craftingSystem == null}");

        if (craftingSystem == null)
        {
            Debug.LogError("No CraftingSystem found in CraftingUI.");
            return;
        }

        Debug.Log($"HasInputAuthority: {craftingSystem.Object.HasInputAuthority}");
        RequestCraft(keyA, keyB);

        ClearSlots(); // opcional pero recomendado

        inventoryUI.ForceRefresh();
    }
    public void RequestCraft(short a, short b)
    {
        var inventory = FindFirstObjectByType<NwInventoryUI>()?.GetTargetInventory();
        if (inventory == null) return;

        var crafting = inventory.GetComponent<CraftingSystem>();
        if (crafting == null) return;

        Debug.Log($"HasInputAuthority: {crafting.Object.HasInputAuthority}");

        crafting.RPC_RequestCraft(a, b);
    }
    //ClearSlots();


    void ClearSlots()
    {
        slotA.Clear();
        slotB.Clear();
    }
}