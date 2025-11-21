using System.Linq;
using Fusion;
using UnityEngine;

public class InventoryDebug : NetworkBehaviour
{
    [SerializeField] NetworkInventory inv;
    [SerializeField] PlayerStats stats;

    private void Update()
    {
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
        inv = GetComponent<NetworkInventory>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        // Solo el dueño puede debuggear su propio personaje
        if (!Object.HasInputAuthority) return;

        // Mostrar items
        if (Input.GetKeyDown(KeyCode.I))
            Debug_ShowInventory();

        // Equipar todo
        //if (Input.GetKeyDown(KeyCode.G))

        // Desequipar todo
        if (Input.GetKeyDown(KeyCode.U))
            Debug_UnequipAll();

        // Debug de stats
        if (Input.GetKeyDown(KeyCode.P))
        {           
            
            Debug.LogError("equipment requested ");

            Debug_ShowStats();            

            Debug_EquipAll();
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

    private void Debug_EquipAll()
    {
        foreach (var id in inv.Items)
        {        
            Debug.Log(id);

            inv.RPC_ServerEquipmentRequest(id);}

        Debug.Log($"[{Object.InputAuthority}] Equipado TODO");
    }

    private void Debug_UnequipAll()
    {
        foreach (var id in inv.EquipedItems.ToList())
            inv.RPC_ServerUnequipmentRequest(id);

        Debug.Log($"[{Object.InputAuthority}] Desequipado TODO");
    }
}

