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
            if (r == null) continue;
            
            Debug.Log($"[DB] Comparando receta: {r.itemA}+{r.itemB} vs {a}+{b}");
            if ((r.itemA == a && r.itemB == b) ||
                (r.itemA == b && r.itemB == a))
            {
                Debug.Log($"[DB] Match encontrado! Resultado: {r.resultItemKey}");
                return r;
            }
        }
        Debug.LogWarning($"[DB] No hay receta para {a}+{b}");
        return null;
    }
}
