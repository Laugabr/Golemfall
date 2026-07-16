using Fusion;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class NetworkInventory : NetworkBehaviour
{
    [Header("Descarte al mundo")]
    [Tooltip("Altura sobre el player a la que aparece el item descartado. " +
             "Spawnear sobre la cabeza garantiza que nunca caiga en agua, KillZone " +
             "ni geometría rara: siempre es recuperable con un salto en el lugar.")]
    [SerializeField] private float dropHeight = 1.5f;

    [Tooltip("Radio del jitter horizontal del spawn. Evita que varios descartes " +
             "seguidos queden encastrados exactamente en el mismo punto.")]
    [SerializeField] private float dropRadius = 0.3f;

    // IsDirty ya no es [Networked] — es local, se setea via RPC
    public bool IsDirty { get; set; }

    [Networked, Capacity(9)]
    public NetworkLinkedList<InventorySlot> Items => default;

    public List<InventorySlot> LocalItems;

    [Networked, Capacity(3)]
    public NetworkLinkedList<short> EquippedItems => default;

    #region SERVER

    public bool AddItem_Server(short itemKey, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return false;

        if (Items.Count >= 9)
        {
            Debug.Log("[SERVER] full inventory");
            return false;
        }

        if (ItemData.GetItem(itemKey) == null)
        {
            Debug.Log("[SERVER] invalid item");
            return false;
        }

        Items.Add(InventorySlot.Create(null, itemKey));
        RPC_UpdateLocalInventory();
        RPC_NotifyInventoryChanged();

        Debug.Log($"[SERVER] Item added ({Items.Count}/9)");
        BasicEventsManager.OnInventoryCountChanged?.Invoke(Items.Count);
        return true;
    }

    // Remueve UNA instancia de itemKey. Si era la última que quedaba y el item
    // estaba equipado, lo desequipa y refresca stats — así un item que ya no se
    // posee nunca sigue otorgando bonus.
    //
    // Vivía como método privado en CraftingSystem. Se movió acá porque mutar el
    // inventario es responsabilidad de NetworkInventory, no del sistema de crafteo,
    // y porque el descarte necesita exactamente la misma regla (dos copias de esta
    // lógica se desincronizarían tarde o temprano).
    //
    // A propósito NO notifica al cliente: el llamador decide cuándo re-sincronizar,
    // para poder agrupar varias remociones en un solo refresh (ej: los dos
    // materiales de un craft).
    public bool RemoveItem_Server(short itemKey)
    {
        if (!Object.HasStateAuthority) return false;

        foreach (var slot in Items)
        {
            if (slot.itemKey != itemKey) continue;

            Items.Remove(slot);

            if (!Items.Any(s => s.itemKey == itemKey) && EquippedItems.Contains(itemKey))
            {
                EquippedItems.Remove(itemKey);
                GetComponent<PlayerStats>()?.RefreshStats();
            }
            return true;
        }
        return false;
    }

    #endregion

    #region CLIENT

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_UpdateLocalInventory(RpcInfo info = default)
    {
        Debug.Log($"[RPC] UpdateLocalInventory recibido — {Items.Count} items");
        LocalItems = new List<InventorySlot>(Items);
    }

    // Notifica al cliente que el inventario cambió — se setea una sola vez, no en loop
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_NotifyInventoryChanged(RpcInfo info = default)
    {
        Debug.Log("[CLIENT] IsDirty = true");
        IsDirty = true;
    }

    #endregion

    #region EQUIP

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestEquip(short itemKey)
    {
        if (!Object.HasStateAuthority) return;

        if (!Items.Any(s => s.itemKey == itemKey)) return;

        // no exceder la capacidad de EquippedItems (Capacity(3)).
        // El !Contains evita bloquear un re-equip de algo ya equipado.
        if (EquippedItems.Count >= 3 && !EquippedItems.Contains(itemKey))
        {
            Debug.Log("[SERVER] equip slots full");
            return;
        }

        if (!EquippedItems.Contains(itemKey))
        {
            Debug.Log($"[SERVER] Item equipped: {itemKey}");
            EquippedItems.Add(itemKey);
            GetComponent<PlayerStats>().RefreshStats();
            RPC_NotifyInventoryChanged();
        }
        else
        {
            Debug.Log($"[SERVER] Item already equipped: {itemKey}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestUnequip(short itemKey, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;

        if (EquippedItems.Remove(itemKey))
        {
            Debug.Log($"[SERVER] Item unequipped: {itemKey}");
            GetComponent<PlayerStats>().RefreshStats();
            RPC_NotifyInventoryChanged();
        }
        else
        {
            Debug.Log($"[SERVER] Item not equipped: {itemKey}");
        }
    }


    #endregion

    #region DISCARD

    // Saca el item del inventario y lo devuelve al mundo como PickableItem.
    //
    // El cliente solo manda la key: la posición de spawn la decide el server,
    // así un cliente no puede pedir que el item aparezca donde se le antoje.
    //
    // Aplica tanto a items del inventario como equipados: RemoveItem_Server ya
    // se encarga de desequipar y refrescar stats si era la última instancia.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDiscard(short itemKey, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;

        if (!Items.Any(s => s.itemKey == itemKey))
        {
            Debug.Log($"[SERVER] Discard rechazado — el item {itemKey} no está en el inventario");
            ResyncClient();
            return;
        }

        var data = ItemData.GetItem(itemKey);
        if (data == null || data.worldPrefab == null)
        {
            // Se valida ANTES de remover: un SO mal configurado no puede hacer
            // desaparecer un item para siempre. Se conserva y se re-sincroniza
            // el cliente, que ya lo borró de la UI de forma optimista.
            Debug.LogWarning($"[SERVER] Discard abortado — ItemData {itemKey} sin worldPrefab. El item se conserva.");
            ResyncClient();
            return;
        }

        if (!RemoveItem_Server(itemKey)) { ResyncClient(); return; }

        Vector2 jitter = UnityEngine.Random.insideUnitCircle * dropRadius;
        Vector3 dropPos = transform.position
                          + Vector3.up * dropHeight
                          + new Vector3(jitter.x, 0f, jitter.y);

        // Mismo patrón que DestructibleObject: spawn server-side, sin física.
        // PickableItem.Spawned() congela SpawnedPosition y los proxies la copian,
        // así el item queda quieto y sincronizado para todos los peers, con su
        // ProximityInteractor activo — cualquier jugador puede recogerlo.
        Runner.Spawn(data.worldPrefab, dropPos, Quaternion.identity);

        ResyncClient();
        RPC_NotifyItemDiscarded(itemKey);

        BasicEventsManager.OnInventoryCountChanged?.Invoke(Items.Count);
        Debug.Log($"[SERVER] Item descartado al mundo: {itemKey} ({Items.Count}/9)");
    }

    // Server -> InputAuthority. Solo el dueño del inventario descuenta el tracking.
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_NotifyItemDiscarded(short itemKey)
    {
        var data = ItemData.GetItem(itemKey);
        if (data == null) return;

        // Contrapeso del +1 que CharacterPickUp.TryPickUp dispara al recolectar.
        // Sin esto, tirar y volver a levantar el mismo item de misión inflaría el
        // contador del objetivo indefinidamente.
        //
        // Tiene que correr acá y no en el server: TrackEvents es un Action estático,
        // o sea que el tracking es client-local, igual que el +1 del pickup.
        TrackEvents.OnTrackEvent?.Invoke(GameEventType.CollectItem, -1, data.missionKey);
    }

    private void ResyncClient()
    {
        RPC_UpdateLocalInventory();
        RPC_NotifyInventoryChanged();
    }

    #endregion
}

// Static event manager for inventory actions
public static class InventoryEventsManager
{
    public static System.Action<string> OnItemEquiped;
    public static System.Action<string> OnItemUnequiped;
}

[System.Serializable]
public struct InventorySlot : INetworkStruct
{
    public short itemKey;

    public readonly ItemData GetItem() => ItemData.GetItem(itemKey);

    public ItemData GetData()
    {
        if (ResourcesManager.instance == null || ResourcesManager.instance.inventoryItemBank == null)
            return null;
        return ResourcesManager.instance.inventoryItemBank.GetValue<ItemData>(itemKey);
    }

    public static InventorySlot Create(ItemData item = null, short id = -1)
    {
        short finalKey = id;
        if (item != null && ResourcesManager.instance != null)
            finalKey = ResourcesManager.instance.inventoryItemBank.GetKey(item);
        return new InventorySlot { itemKey = finalKey };
    }

    public readonly bool IsItem(ItemData item)
    {
        return item != null && ItemData.GetKey(item) == itemKey;
    }
}