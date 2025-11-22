using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;

/*
  EquipSlotDrop
 
  Handles dropping an item into an equipment slot.
  Prevents overriding occupied slots, moves the dragged item into the slot UI,
  and sends an equipment request to the server through the player's NetworkInventory.
 */

public class EquipSlotDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag.GetComponent<ItemSlotDrag>();
        if (dragged == null) return; // Invalid drop: no draggable item

        // Prevent equipping if slot is already occupied
        if (transform.childCount > 0) return;

        // Move the UI element to this equipment slot
        dragged.transform.SetParent(transform);
        dragged.transform.localPosition = Vector3.zero;

        // Retrieve the item's unique ID
        string itemID = dragged.GetItemID();
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogError("[EquipSlotDrop] El item no tiene ID.");
            return;
        }

        // Find the player with input authority (the local player)
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

        // Get the player's NetworkInventory component

        var inventory = localPlayer.GetComponent<NetworkInventory>();

        if (inventory == null)
        {
            Debug.LogError("[EquipSlotDrop] El player no tiene NetworkInventory.");
            return;
        }

        // Request server-side equipment

        Debug.Log("[EquipSlotDrop] RPC equip request: " + itemID);
        inventory.RPC_ServerEquipmentRequest(itemID);
    }
}