using UnityEngine;

[CreateAssetMenu(menuName = "Crafting/Recipe (2 Items)")]
public class CraftingRecipe : ScriptableObject
{
    public short itemA;
    public short itemB;

    public short resultItemKey;
}