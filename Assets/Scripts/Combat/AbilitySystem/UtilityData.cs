using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Utility Ability", fileName = "NewUtilityAbility")]
public class UtilityAbilityData : Ability
{
    public GameObject aoePrefab;
    public float aoeRadius = 5f;
    public float healMultiplier = 1f; // heal = damage * healMultiplier
    public float activeTime = 2f;     // cuánto dura el AOE visible
}