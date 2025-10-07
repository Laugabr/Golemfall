using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Configuración")]
    public int capacity = 5;
    [Tooltip("Transform del jugador (para spawnear items al arrojar)")]
    public Transform playerTransform;

    private List<ItemData> items = new List<ItemData>();

    public event Action OnInventoryChanged;
    public event Action<ItemData> OnItemAdded; // notifica qué item fue agregado

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool HasSpace() => items.Count < capacity;

    public bool AddItem(ItemData item)
    {
        if (item == null) return false;
        if (!HasSpace()) return false;
        items.Add(item);
        OnInventoryChanged?.Invoke();
        OnItemAdded?.Invoke(item);
        return true;
    }

    public ItemData GetItemAt(int index)
    {
        if (index < 0 || index >= items.Count) return null;
        return items[index];
    }

    public int Count => items.Count;

    // Remueve y devuelve el item (útil antes de equipar)
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

    // Arroja el item al mundo (usa playerTransform para posicion)
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

    // Helper: intenta agregar, si no hay espacio, spawnear en world cerca del player
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
        return items.IndexOf(item);
    }

}

