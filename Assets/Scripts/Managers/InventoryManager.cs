using System;
using System.Collections.Generic;
using UnityEngine;

/*
 InventoryManager
 
  Local (non-networked) inventory system that stores ItemData objects.
  Handles adding, removing, replacing, and spawning items in the world.
  Supports capacity limits and events for UI updates or external listeners.
  Implemented as a simple singleton for easy access across the project.
 */

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Configuración")]
    public int capacity = 12; // Maximum number of items
    [Tooltip("Transform del jugador (para spawnear items al arrojar)")]
    public Transform playerTransform;

    private List<ItemData> items = new List<ItemData>(); // Local item storage

    public event Action OnInventoryChanged; // Fired whenever inventory contents change
    public event Action<ItemData> OnItemAdded; // Fired when a specific item is added

    void Awake()
    {
        // Basic singleton pattern

        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool HasSpace() => items.Count < capacity;


    public bool AddItem(ItemData item)
    {
        // Validate item and capacity

        if (item == null) return false;
        if (!HasSpace()) return false;
    

        items.Add(item);
        Debug.Log("Item agregado al inventario: " + item.name);
        OnInventoryChanged?.Invoke();
        OnItemAdded?.Invoke(item);
        return true;
    }


    public ItemData GetItemAt(int index)
    {
        // Safely return item or null if out of range

        if (index < 0 || index >= items.Count) return null;
        return items[index];
    }

    public int Count => items.Count; // Number of stored items

    // Removes item at index and returns it (useful for equipping logic)
    public ItemData RemoveAndReturn(int index)
    {
        if (index < 0 || index >= items.Count) return null;
        ItemData it = items[index];
        items.RemoveAt(index);
        OnInventoryChanged?.Invoke();
        return it;
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= items.Count) return false;
        items.RemoveAt(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    // Spawns the removed item into the world near the player
    public void ThrowItemFromInventory(int inventoryIndex)
    {
        ItemData it = RemoveAndReturn(inventoryIndex);
        if (it == null) return;
        if (it.worldPrefab != null && playerTransform != null)
        {
            Vector3 spawnPos = playerTransform.position + playerTransform.forward * 1.2f + Vector3.up * 0.5f;
            Instantiate(it.worldPrefab, spawnPos, Quaternion.identity);
        }
    }

    // Attempts to add an item; if no space, spawns it into the world instead
    public void AddOrSpawn(ItemData item)
    {
        if (!AddItem(item))
        {
            if (item.worldPrefab != null && playerTransform != null)
            {
                Vector3 spawnPos = playerTransform.position + playerTransform.forward * 1.2f + Vector3.up * 0.5f;
                Instantiate(item.worldPrefab, spawnPos, Quaternion.identity);
            }
        }
    }
    public int FindIndex(ItemData item)
    {
        // Returns index of given item or -1 if not found

        return items.IndexOf(item);
    }
    public void ReplaceItemAt(int index, ItemData newItem)
    {
        // Overwrites item at index and notifies listeners

        if (index >= 0 && index < items.Count)
        {
            items[index] = newItem;
            OnInventoryChanged?.Invoke();
        }
    }


}

