using Fusion;
using UnityEngine;

public class AbilityHolder : NetworkBehaviour
{
    [SerializeField] private Ability[] abilities;

    [Networked, Capacity(4)]
    private NetworkArray<float> cooldowns => default;

    [Networked, Capacity(4)]
    private NetworkArray<float> activeTimers => default;

    [Networked, Capacity(4)]
    private NetworkArray<AbilityState> states => default;

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        for (int i = 0; i < abilities.Length; i++)
        {
            switch (states[i])
            {
                case AbilityState.Active:

                    float active = activeTimers[i];
                    active -= Runner.DeltaTime;
                    activeTimers.Set(i, active);

                    if (active <= 0)
                    {
                        states.Set(i, AbilityState.Cooldown);
                        cooldowns.Set(i, abilities[i].cooldownTime);
                    }
                    break;

                case AbilityState.Cooldown:

                    float cd = cooldowns[i];  //Read the array
                    cd -= Runner.DeltaTime;   //Modify the value
                    cooldowns.Set(i, cd);     // Set it again

                    if (cd <= 0)
                        states.Set(i, AbilityState.Ready);
                    break;
            }
        }
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestUseAbility(int index, Vector3 direction, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;

        if (index < 0 || index >= abilities.Length) return;

        if (states.Get(index) != AbilityState.Ready)
        {
            Debug.Log($"[SERVER] Skill {index} en cooldown");
            return;
        }

        var ability = abilities[index];

        if (ability == null)
        {
            Debug.LogError("Ability null");
            return;
        }

        Debug.Log($"[SERVER] Player {info.Source} usa skill {index}");

        if (ability is ProjectileAbility proj)
        {
            ProjectileRuntime.Execute(proj, Runner, gameObject, direction);
        }

        states.Set(index, AbilityState.Active);
        activeTimers.Set(index, ability.activeTime);
    }
    
}
enum AbilityState
{
    Ready,
    Active,
    Cooldown
}

