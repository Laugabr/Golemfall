using Fusion;
using UnityEngine;

public class AbilityHolder : NetworkBehaviour
{
    [SerializeField] private Ability[] abilities;
    [SerializeField] private GameObject[] fakePrefabs; // ← mismo orden que abilities[]

    [Networked, Capacity(4)] private NetworkArray<float> cooldowns => default;
    [Networked, Capacity(4)] private NetworkArray<float> activeTimers => default;
    [Networked, Capacity(4)] private NetworkArray<AbilityState> states => default;

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        for (int i = 0; i < abilities.Length; i++)
        {
            switch (states[i])
            {
                case AbilityState.Active:
                    float active = activeTimers[i] - Runner.DeltaTime;
                    activeTimers.Set(i, active);
                    if (active <= 0)
                    {
                        states.Set(i, AbilityState.Cooldown);
                        cooldowns.Set(i, abilities[i].cooldownTime);
                    }
                    break;

                case AbilityState.Cooldown:
                    float cd = cooldowns[i] - Runner.DeltaTime;
                    cooldowns.Set(i, cd);
                    if (cd <= 0)
                        states.Set(i, AbilityState.Ready);
                    break;
            }
        }
    }

    // ── Punto de entrada desde tu InputHandler ──────────────────────────────
    public void TryUseAbility(int index, Vector3 direction)
    {
        if (index < 0 || index >= abilities.Length) return;
        if (states.Get(index) == AbilityState.Cooldown) return;

        if (Object.HasStateAuthority)
        {
            // Host/servidor: ejecuta directo
            ExecuteAuthority(index, direction);
            return;
        }

        if (Object.HasInputAuthority)
        {
            // Cliente: feedback visual inmediato + pedido al servidor
            SpawnFakeProjectile(index, direction);
            RPC_RequestUseAbility(index, direction);
        }
    }

    // ── RPC cliente → servidor ───────────────────────────────────────────────
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestUseAbility(int index, Vector3 direction, RpcInfo info = default)
    {
        if (index < 0 || index >= abilities.Length) return;
        if (states.Get(index) == AbilityState.Cooldown)
        {
            Debug.Log($"[SERVER] Skill {index} en cooldown");
            return;
        }

        var ability = abilities[index];
        if (ability == null) { Debug.LogError("Ability null"); return; }

        Debug.Log($"[SERVER] Player {info.Source} usa skill {index}");

        if (ability is ProjectileAbility proj)
            ProjectileRuntime.Execute(proj, Runner, Object, direction);

        states.Set(index, AbilityState.Active);
        activeTimers.Set(index, ability.activeTime);
    }

    // ── Ejecución directa con state authority (host o servidor) ─────────────
    private void ExecuteAuthority(int index, Vector3 direction)
    {
        if (states.Get(index) == AbilityState.Cooldown) return;

        var ability = abilities[index];
        if (ability == null) return;

        if (ability is ProjectileAbility proj)
            ProjectileRuntime.Execute(proj, Runner, Object, direction);

        states.Set(index, AbilityState.Active);
        activeTimers.Set(index, ability.activeTime);
    }

    // ── Proyectil visual local (sin red) ────────────────────────────────────
    private void SpawnFakeProjectile(int index, Vector3 direction)
    {
        if (fakePrefabs == null || index >= fakePrefabs.Length) return;
        var prefab = fakePrefabs[index];
        if (prefab == null) return;

        var ability = abilities[index] as ProjectileAbility;
        if (ability == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * 1f;
        var go = Instantiate(prefab, spawnPos, Quaternion.LookRotation(direction));
        var fake = go.AddComponent<FakeProjectile>();

        uint ownerId = Object.Id.Raw;
        fake.Initialize(ownerId, direction, ability.projectileSpeed, ability.activeTime);
    }

    // ── Helpers para UI ─────────────────────────────────────────────────────
    //public AbilityState GetState(int index) => states.Get(index);
    //public float GetCooldown(int index) => cooldowns.Get(index);
}

enum AbilityState
{
    Ready,
    Active,
    Cooldown
}