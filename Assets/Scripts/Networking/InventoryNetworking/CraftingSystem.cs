using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;
using System.Linq;


public class CraftingSystem : NetworkBehaviour
{
    [SerializeField] private CraftingDatabase database;

    [Header("Feedback")]
    [Tooltip("NotificationData que define layout/prioridad/duración/template del aviso de craft exitoso. " +
             "El ícono se sobrescribe en runtime con el del item crafteado. " +
             "Template sugerido: \"Crafteaste {0}\\n{1}\"  (donde {0}=nombre, {1}=stats).")]
    [SerializeField] private NotificationData craftedNotification;

    public void TryCraft(short itemA, short itemB)
    {
        if (!Object.HasStateAuthority) return;
        if (database == null) { Debug.LogError("No CraftingDatabase"); return; }

        var inv = GetComponent<NetworkInventory>();
        if (inv == null) { Debug.LogError("No hay NetworkInventory"); return; }

        var recipe = database.Find(itemA, itemB);
        if (recipe == null)
        {
            Debug.Log("No existe receta");

            inv.RPC_UpdateLocalInventory();      // 🔥 RE-SYNC
            inv.RPC_NotifyInventoryChanged();    // 🔥 FORZAR REFRESH

            return;
        }

        if (!HasMaterials(inv, recipe))
        {
            Debug.Log("Faltan materiales");

            inv.RPC_UpdateLocalInventory();
            inv.RPC_NotifyInventoryChanged();

            return;
        }
        // 1. Remover materiales (incluye desequipar si era la última instancia)
        inv.RemoveItem_Server(recipe.itemA);
        inv.RemoveItem_Server(recipe.itemB);

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

        // Notificación de feedback al cliente dueño del craft
        RPC_NotifyCraftSuccess(recipe.resultItemKey);

        Debug.Log("[CRAFT] NotifyInventoryChanged enviado");
        var resultData = ItemData.GetItem(recipe.resultItemKey);
        Debug.Log($"[CRAFT] Resultado obtenido: {resultData?.name ?? "item desconocido"} (key={recipe.resultItemKey})");
        Debug.Log("[CRAFT] Exitoso");
        TrackEvents.OnTrackEvent?.Invoke(GameEventType.CraftItem, 1, "");
    }

    // Consulta read-only del resultado de una receta, para la preview de UI.
    // No consume materiales ni modifica estado. El craft real sigue yendo
    // por RPC_RequestCraft, que revalida todo en el server.
    public short PreviewResult(short a, short b)
    {
        if (database == null) return -1;
        var recipe = database.Find(a, b);
        return recipe != null ? recipe.resultItemKey : (short)-1;
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestCraft(short itemA, short itemB, RpcInfo info = default)
    {
        // TryCraft ya valida HasStateAuthority, database, NetworkInventory,
        // existencia de receta y HasMaterials, y notifica al cliente en cada
        // caso de fallo. Esta RPC solo delega.
        TryCraft(itemA, itemB);
    }

    // Server → InputAuthority. Solo el cliente dueño del player ve la notificación.
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_NotifyCraftSuccess(short resultKey)
    {
        if (craftedNotification == null)
        {
            Debug.LogWarning("[CraftingSystem] craftedNotification no asignada en el Inspector.");
            return;
        }

        var data = ItemData.GetItem(resultKey);
        if (data == null) return;

        if (NotificationManager.Instance == null) return;

        string statsText = FormatStats(data.stats);
        NotificationManager.Instance.Show(craftedNotification, data.icon, data.displayName, statsText);
    }

    // Replicado a propósito desde ItemSlot.FormatStats para no acoplar
    // CraftingSystem al componente de UI.
    private static string FormatStats(Stats stats)
    {
        if (stats == null || stats.statInfo.Count == 0) return "";

        string result = "";
        foreach (var s in stats.statInfo)
            result += $"{s.statType}: {s.statValue:+#;-#;0}\n";
        return result.TrimEnd('\n');
    }
}