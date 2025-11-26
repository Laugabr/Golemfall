using UnityEngine;
using UnityEngine.EventSystems;
using Fusion;

/*
  CollectedSlotDrop

  Handles the logic for dropping an item into a "collected" inventory slot.
  Prevents dropping into occupied slots, validates equipment origin,
  and triggers unequip requests when necessary.
 */

public class CollectedSlotDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag.GetComponent<ItemSlotDrag>();
        if (dragged == null) return; // Drop is invalid if object has no draggable slot


        // Prevent dropping if this slot already contains an item

        if (transform.childCount > 0) return;

        // Check if the dragged item originated from an equipment slot

        bool cameFromEquipSlot = dragged.originalParent.GetComponent<EquipSlotDrop>() != null;
        string itemID = dragged.GetItemID();

        if (cameFromEquipSlot)
        {
            // Find local player to send unequip RPC
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
            inventory.RPC_ServerUnequipmentRequest(itemID); // Notify server to unequip the item
        }

        // Move dragged item into this collected slot
        dragged.transform.SetParent(transform);
        dragged.transform.localPosition = Vector3.zero;


        // notify inventory or equipment manager here if needed
    }
}

