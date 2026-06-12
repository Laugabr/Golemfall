using UnityEngine;

[CreateAssetMenu(menuName = "Crafting/Recipe (2 Items)")]
public class CraftingRecipe : ScriptableObject
{
    [SerializeField] private ItemData _itemA;
    [SerializeField] private ItemData _itemB;
    [SerializeField] private ItemData _resultItem;

    // Contrato público intacto: el resto del sistema sigue leyendo `short`.
    // La key se resuelve dinámicamente desde el ResourceBank en runtime,
    // así reordenar el bank ya no rompe las recetas.
    public short itemA         => _itemA      != null ? ItemData.GetKey(_itemA)      : (short)-1;
    public short itemB         => _itemB      != null ? ItemData.GetKey(_itemB)      : (short)-1;
    public short resultItemKey => _resultItem != null ? ItemData.GetKey(_resultItem) : (short)-1;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_itemA == null)      Debug.LogWarning($"[CraftingRecipe] '{name}': slot Item A vacío.", this);
        if (_itemB == null)      Debug.LogWarning($"[CraftingRecipe] '{name}': slot Item B vacío.", this);
        if (_resultItem == null) Debug.LogWarning($"[CraftingRecipe] '{name}': slot Result vacío.", this);
    }
#endif
}