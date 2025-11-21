using UnityEngine;
using UnityEngine.EventSystems;
using Fusion;

public class CollectedSlotDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag.GetComponent<ItemSlotDrag>();
        if (dragged == null) return;

        // Evitar si el slot ya está ocupado
        if (transform.childCount > 0) return;

        bool cameFromEquipSlot = dragged.originalParent.GetComponent<EquipSlotDrop>() != null;
        string itemID = dragged.GetItemID();

        if (cameFromEquipSlot)
        {
        var players = FindObjectsOfType<PlayerStats>();
        PlayerStats localPlayer = null;

        foreach (var p in players)
            if (p.Object != null && p.Object.HasInputAuthority)
                localPlayer = p;

        if (localPlayer == null)
        {
            Debug.LogError("[EquipSlotDrop] No se encontró player local.");
            return;
        }

        var inventory = localPlayer.GetComponent<NetworkInventory>();

        if (inventory == null)
        {
            Debug.LogError("[EquipSlotDrop] El player no tiene NetworkInventory.");
            return;
        }

        Debug.Log("[EquipSlotDrop] RPC equip request: " + itemID);
        inventory.RPC_ServerUnequipmentRequest(itemID);
        }

        dragged.transform.SetParent(transform);
        dragged.transform.localPosition = Vector3.zero;


        // Aquí se podría notificar al inventario que ya no está equipado
        // Ej: EquipManager.Instance.UnequipItem(dragged.GetItemData());
    }
}

