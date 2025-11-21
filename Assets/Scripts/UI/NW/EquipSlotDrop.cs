using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;

public class EquipSlotDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag.GetComponent<ItemSlotDrag>();
        if (dragged == null) return;

        // Evitar reemplazar si ya hay algo
        if (transform.childCount > 0) return;

        // Mover en la UI
        dragged.transform.SetParent(transform);
        dragged.transform.localPosition = Vector3.zero;

        // Obtener ID real
        string itemID = dragged.GetItemID();
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogError("[EquipSlotDrop] El item no tiene ID.");
            return;
        }

        // Buscar player local (input authority)
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

        // Mandar request al servidor
        Debug.Log("[EquipSlotDrop] RPC equip request: " + itemID);
        inventory.RPC_ServerEquipmentRequest(itemID);
    }
}