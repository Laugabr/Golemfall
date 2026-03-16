using System.Linq;
using Fusion;
using UnityEngine;

/*
  InventoryDebug

  Monitors NetworkInventory changes at runtime.
  Logs updated inventory items whenever the inventory is marked as dirty.
  Client-side debug helper for inspecting and manipulating a player's inventory.
  Allows showing items, showing stats, and unequipping all items through hotkeys.
  Only runs for the object with input authority (the local player).


public class InventoryDebug : NetworkBehaviour
{
    [SerializeField] NetworkInventory inv;
    [SerializeField] PlayerStats stats;

    private void Update()
    {
        // Log inventory changes when marked dirty

        if (inv != null && inv.IsDirty)
        {
            inv.IsDirty = false;
            Debug.Log("INVENTARIO ACTUALIZADO:");
            foreach (var it in inv.Items)
                Debug.Log(" - " + it);
        }

    }
}public class InventoryDebugTester : NetworkBehaviour
{
    private NetworkInventory inv;
    private PlayerStats stats;

    private void Awake()
    {
        // Cache inventory and stats references

        inv = GetComponent<NetworkInventory>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        // Only the player with input authority can debug themselves
        if (!Object.HasInputAuthority) return;

        // Show inventory contents
        if (Input.GetKeyDown(KeyCode.I))
            Debug_ShowInventory();

        // Debugger(Equip)
        //if (Input.GetKeyDown(KeyCode.G))

        // Unequip all equipped items
        if (Input.GetKeyDown(KeyCode.U))
            Debug_UnequipAll();

        // Show current stats
        if (Input.GetKeyDown(KeyCode.P))
        {           
            
            Debug.LogError("equipment requested ");

            Debug_ShowStats();            

            }
    }

    private void Debug_ShowInventory()
    {
        Debug.Log($"[{Object.InputAuthority}] INVENTORY → {string.Join(", ", inv.Items)}");
        Debug.Log($"[{Object.InputAuthority}] EQUIPPED → {string.Join(", ", inv.EquipedItems)}");
    }

    private void Debug_ShowStats()
    {
        string s = $"[{Object.InputAuthority}] STATS: ";
        foreach (var st in stats.localStats)
            s += $"{st.statType}={st.statValue} ";

        Debug.Log(s);
    }


    private void Debug_UnequipAll()
    {
        // Convert to list to avoid modifying the collection while iterating

        foreach (var id in inv.EquipedItems.ToList())
            inv.RPC_ServerUnequipmentRequest(id);

        Debug.Log($"[{Object.InputAuthority}] Desequipado TODO");
    }
}


*/