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
        if (playerObject != null)
        {
            playerMovementComp = playerObject.GetComponent(typeof(MonoBehaviour));
            // buscamos el componente PlayerMovement por tipo
            var pm = playerObject.GetComponent("PlayerMovement");
            if (pm != null)
            {
                playerMovementComp = pm;
                var type = pm.GetType();
                moveSpeedField = type.GetField("moveSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
                jumpHeightField = type.GetField("jumpHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            else
            {
                Debug.LogWarning("PlayerMovement no encontrado en playerObject. No se aplicarán multiplicadores.");
            }
        }

        // asegurar tamaños
        if (equipped == null || equipped.Length != slots) equipped = new ItemData[slots];
        if (equipCubes == null || equipCubes.Length != slots)
            Debug.LogWarning("Asigná equipCubes en inspector (dos cubos)");

        // esconder cubos al inicio
        for (int i = 0; i < Mathf.Min(slots, equipCubes.Length); i++)
            if (equipCubes[i] != null) equipCubes[i].SetActive(false);
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
}


