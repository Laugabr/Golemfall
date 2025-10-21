using System.Reflection;
using UnityEngine;
using System;

public class EquipManager : MonoBehaviour
{
    public static EquipManager Instance { get; private set; }

    [Header("Slots")]
    public int slots = 2;
    public ItemData[] equipped; // tamaño = slots

    [Header("Referencias de jugador")]
    [Tooltip("Referencia al Player GameObject que tiene PlayerMovement")]
    public GameObject playerObject;
    [Tooltip("Dos cubos hijos (desactivados por defecto). Size debe = slots")]
    public GameObject[] equipCubes;

    private Component playerMovementComp;
    private FieldInfo moveSpeedField;
    private FieldInfo jumpHeightField;

    public event Action<ItemData, bool> OnEquipChanged; // bool = true si se equipó, false si se quitó

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // asegurar tamaños
        if (equipped == null || equipped.Length != slots) equipped = new ItemData[slots];
        if (equipCubes == null || equipCubes.Length != slots)
            Debug.LogWarning("Asigná equipCubes en inspector (dos cubos)");

        // esconder cubos al inicio
        for (int i = 0; i < Mathf.Min(slots, equipCubes.Length); i++)
            if (equipCubes[i] != null) equipCubes[i].SetActive(false);

        // Suscribirse a los cambios de equipamiento
        OnEquipChanged += HandleEquipChange;
    }

    // Equipar item removiéndolo del inventario (inventoryIndex -> equipIndex)
    public void EquipFromInventory(int inventoryIndex, int equipIndex)
    {
        if (inventoryIndex < 0 || equipIndex < 0 || equipIndex >= slots) return;
        ItemData it = InventoryManager.Instance.RemoveAndReturn(inventoryIndex);
        if (it == null) return;

        // si ya hay item en equip slot, lo devolvemos al inventario
        if (equipped[equipIndex] != null)
        {
            InventoryManager.Instance.AddOrSpawn(equipped[equipIndex]);
        }

        equipped[equipIndex] = it;
        UpdateVisuals();
        OnEquipChanged?.Invoke(it, true);
    }

    public void UnEquipToInventory(int equipIndex)
    {
        if (equipIndex < 0 || equipIndex >= slots) return;
        ItemData it = equipped[equipIndex];
        if (it == null) return;

        // devolver al inventario
        InventoryManager.Instance.AddOrSpawn(it);
        equipped[equipIndex] = null;
        UpdateVisuals();
        OnEquipChanged?.Invoke(it, false);
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < slots; i++)
        {
            if (equipCubes != null && i < equipCubes.Length && equipCubes[i] != null)
            {
                if (equipped[i] != null)
                {
                    equipCubes[i].SetActive(true);
                    var rend = equipCubes[i].GetComponent<Renderer>();
                    if (rend != null) rend.material.color = equipped[i].color;
                }
                else
                {
                    equipCubes[i].SetActive(false);
                }
            }
        }
    }
    private void HandleEquipChange(ItemData item, bool equipped)
    {
        if (item == null || item.stats == null || item.stats.statInfo == null) return;

        CharacterStats stats = playerObject.GetComponent<CharacterStats>();
        if (stats == null)
        {
            Debug.LogWarning("El jugador no tiene CharacterStats, no se pueden modificar stats.");
            return;
        }
        stats.LogCurrentStats();

        string action = equipped ? "Equipado" : "Desequipado";
        string debugMsg = $"{action}: {item.displayName}\n";

        foreach (var s in item.stats.statInfo)
        {
            var statEntry = stats.localStats.Find(ls => ls.statType == s.statType);
            if (statEntry != null)
            {
                statEntry.statValue += equipped ? s.statValue : -s.statValue;
                debugMsg += $" → {s.statType} {(equipped ? "+" : "-")}{s.statValue} (nuevo valor: {statEntry.statValue})";
            }
            else
            {
                // si no existía la stat en localStats, la agregamos (opcional)
                stats.localStats.Add(new StatInfo(s.statType, s.statValue));
                debugMsg += $" → {s.statType} {(equipped ? "+" : "-")}{s.statValue} (nueva stat añadida)\n";
            }
        }
        Debug.Log(debugMsg);
        stats.LogCurrentStats();
    }
}


