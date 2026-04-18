using UnityEngine;

public class CraftingUI : MonoBehaviour
{
    [SerializeField] private Transform slotA;
    [SerializeField] private Transform slotB;

    private NwInventoryUI inventoryUI;
    private CraftingSystem craftingSystem;


    public void SetCraftingSystem(CraftingSystem targetCraftingSystem)
    {
        inventoryUI = FindFirstObjectByType<NwInventoryUI>();
        craftingSystem = targetCraftingSystem;
    }

    public void OnItemPlaced()
    {
        if (slotA.childCount == 0 || slotB.childCount == 0)
            return;

        var itemA = slotA.GetChild(0).GetComponent<ItemSlot>();
        var itemB = slotB.GetChild(0).GetComponent<ItemSlot>();

        if (itemA == null || itemB == null) return;

        short keyA = ItemData.GetKey(itemA.GetItemData());
        short keyB = ItemData.GetKey(itemB.GetItemData());

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
        foreach (Transform t in slotA) Destroy(t.gameObject);
        foreach (Transform t in slotB) Destroy(t.gameObject);
    }
}