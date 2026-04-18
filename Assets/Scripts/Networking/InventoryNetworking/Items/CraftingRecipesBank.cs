using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(menuName = "Crafting/Database")]
public class CraftingDatabase : ScriptableObject
{
    public List<CraftingRecipe> recipes;

    public CraftingRecipe Find(short a, short b)
    {
        foreach (var r in recipes)
        {
            if ((r.itemA == a && r.itemB == b) ||
                (r.itemA == b && r.itemB == a))
                return r;
        }
        return null;
    }
}
