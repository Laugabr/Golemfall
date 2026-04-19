using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;
using System.Linq;


public class CraftingSystem : NetworkBehaviour
{
    [SerializeField] private CraftingDatabase database;

    public void TryCraft(short itemA, short itemB)
    {
        if (!Object.HasStateAuthority) return;
        if (database == null) { Debug.LogError("No CraftingDatabase"); return; }

        var inv = GetComponent<NetworkInventory>();

        var recipe = database.Find(itemA, itemB);
        if (recipe == null) if (recipe == null)
        {
            Debug.Log("No existe receta");

            inv.RPC_UpdateLocalInventory();      // 🔥 RE-SYNC
            inv.RPC_NotifyInventoryChanged();    // 🔥 FORZAR REFRESH

            return;
        }

        if (inv == null) { Debug.LogError("No hay NetworkInventory"); return; }

        if (!HasMaterials(inv, recipe))
        {
            Debug.Log("Faltan materiales");

            inv.RPC_UpdateLocalInventory();
            inv.RPC_NotifyInventoryChanged();

            return;
        }
        // 1. Remover materiales
        RemoveItem(inv, recipe.itemA);
        RemoveItem(inv, recipe.itemB);

        // 2. Agregar resultado (AddItem_Server ya notifica al cliente)
        bool success = inv.AddItem_Server(recipe.resultItemKey);

        if (!success)
        {
            // Revertir — devolver los materiales
            inv.Items.Add(InventorySlot.Create(null, recipe.itemA));
            inv.Items.Add(InventorySlot.Create(null, recipe.itemB));
            inv.RPC_UpdateLocalInventory();
            inv.RPC_NotifyInventoryChanged();
            Debug.LogWarning("[CRAFT] Falló AddItem — materiales devueltos");
            return;
        }

        inv.RPC_UpdateLocalInventory();
        inv.RPC_NotifyInventoryChanged();
        Debug.Log("[CRAFT] NotifyInventoryChanged enviado");
        var resultData = ItemData.GetItem(recipe.resultItemKey);
        Debug.Log($"[CRAFT] Resultado obtenido: {resultData?.name ?? "item desconocido"} (key={recipe.resultItemKey})");
        Debug.Log("[CRAFT] Exitoso");
    }
    private bool HasMaterials(NetworkInventory inv, CraftingRecipe recipe)
    {
        if (recipe.itemA == recipe.itemB)
        {
            // Necesita al menos 2 del mismo ítem
            return inv.Items.Count(s => s.itemKey == recipe.itemA) >= 2;
        }

        return inv.Items.Any(s => s.itemKey == recipe.itemA) &&
            inv.Items.Any(s => s.itemKey == recipe.itemB);
    }
    void RemoveItem(NetworkInventory inv, short key)
    {
        foreach (var slot in inv.Items)
        {
            if (slot.itemKey == key)
            {
                inv.Items.Remove(slot);
                return;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestCraft(short itemA, short itemB, RpcInfo info = default)
    {
        // 🔒 VALIDACIÓN SERVER SIDE
        if (!Object.HasStateAuthority) return;

        var inv = GetComponent<NetworkInventory>();
        if (inv == null) return;

        var recipe = database.Find(itemA, itemB);
        if (recipe == null) return;

        if (!HasMaterials(inv, recipe)) return;

        // ejecutar craft real
        TryCraft(itemA, itemB);
    }

}

