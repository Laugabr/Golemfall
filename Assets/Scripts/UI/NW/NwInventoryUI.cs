using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NwInventoryUI : MonoBehaviour
{
    [SerializeField] private NetworkInventory targetInventory;
    [SerializeField] private Transform collectedContainer;
    [SerializeField] private Transform equipedContainer;
    [SerializeField] private GameObject slotPrefab;

    // SOLO PARA TEST inicial (antes de conectar el NetworkInventory)
    [SerializeField] private ItemData[] testItems;
    private bool inventoryReady = false;


    private void Update()
{
    if (targetInventory == null) return;

    // El NetworkObject aún no fue spawneado por Fusion
    if (targetInventory.Object == null) return;
    if (!targetInventory.Object.IsValid) return;

    if (targetInventory.IsDirty)
    {
        targetInventory.IsDirty = false;
        RefreshFromNetworkInventory();
    }
}


    public void RefreshCollected(ItemData[] items)
    {
        // Limpia slots anteriores
        foreach (Transform child in collectedContainer)
            Destroy(child.gameObject);

        // Crea slots nuevos
        foreach (var item in items)
        {
            var go = Instantiate(slotPrefab, collectedContainer);
            var slot = go.GetComponent<InventorySlotUI>();
            slot.Setup(item);
        }
    }
    public void RefreshFromNetworkInventory()
    {
        if (targetInventory == null) return;

        // Limpia los slots anteriores
        foreach (Transform child in collectedContainer)
            Destroy(child.gameObject);

        // Para cada itemID en el inventario real
        foreach (var itemID in targetInventory.Items)
        {
            // Cargar el ItemData real
            ItemData data = Resources.Load<ItemData>("DataSO/StatsData/Itemscrafteados/" + itemID);

            if (data != null)
            {
                var go = Instantiate(slotPrefab, collectedContainer);
                var slot = go.GetComponent<InventorySlotUI>();
                slot.Setup(data);
            }
            else
            {
                Debug.LogError("No se encontró ItemData para " + itemID);
            }
        }
    }

}
