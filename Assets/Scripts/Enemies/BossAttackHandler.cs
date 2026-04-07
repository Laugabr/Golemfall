using Fusion;
using UnityEngine;

public class BossAttackHandler : NetworkBehaviour
{
    [SerializeField] private BossAbility[] abilities;

    public void ExecuteAbility(int index)
    {
        if (!Object.HasStateAuthority) return;

        if (index < 0 || index >= abilities.Length) return;

        var ability = abilities[index];

        if (ability == null)
        {
            Debug.LogError("Ability null");
            return;
        }

        ability.Execute(this);
    }
}