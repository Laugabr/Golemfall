using Fusion;
using UnityEngine;

/// <summary>
/// Maneja las habilidades del personaje o enemigo en red.
/// 
/// Flujo para JUGADORES:
///   - Cliente con InputAuthority: spawna un proyectil visual falso inmediato
///     y manda un RPC al servidor para ejecutar la habilidad real.
///   - Servidor (StateAuthority): ejecuta la habilidad directamente.
///
/// Flujo para ENEMIGOS:
///   - Solo el servidor ejecuta habilidades via ExecuteAbilityAuthority()
///     llamado desde EnemyAI. Los enemigos nunca tienen InputAuthority.
///
/// Flujo para HABILIDAD DE CURACIÓN (SecondarySkill / E):
///   - El RPC valida, activa el cooldown y ejecuta el spawn INMEDIATAMENTE
///     en el servidor, igual que los proyectiles.
///   - Se eliminó el patrón HealAtFrame → _healPending porque el delay del
///     RPC causaba que la animación en el servidor ya hubiera pasado el frame
///     11 antes de que _healPending se seteara, impidiendo que el efecto ocurra.
/// </summary>
public class AbilityHolder : NetworkBehaviour
{
    [SerializeField] private Ability[] abilities;
    [SerializeField] private GameObject[] fakePrefabs; // mismo orden que abilities[]

    // Arrays networked para sincronizar el estado de cada habilidad a todos los peers
    [Networked, Capacity(4)] private NetworkArray<float> cooldowns => default;
    [Networked, Capacity(4)] private NetworkArray<float> activeTimers => default;
    [Networked, Capacity(4)] private NetworkArray<AbilityState> states => default;

    private PlayerProgressionVisuals _progression;

    public override void Spawned()
    {
        _progression = GetComponent<PlayerProgressionVisuals>();
    }

    /// <summary>
    /// Actualiza los timers de cooldown y activeTime cada tick.
    /// Solo corre en el servidor para mantener autoridad sobre los estados.
    /// </summary>
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

    /// <summary>
    /// Punto de entrada para usar una habilidad desde el jugador.
    /// </summary>
    public void TryUseAbility(int index, Vector3 direction)
    {
        if (index < 0 || index >= abilities.Length) return;
        if (states.Get(index) == AbilityState.Cooldown) return;

        if (Object.HasStateAuthority)
        {
            ExecuteAbilityAuthority(index, direction);
            return;
        }

        if (Object.HasInputAuthority)
        {
            SpawnFakeProjectile(index, direction);
            RPC_RequestUseAbility(index, direction, 0f);
        }
    }

    /// <summary>
    /// RPC del cliente al servidor para validar y ejecutar la habilidad.
    /// Todas las habilidades (proyectiles Y curación) se ejecutan inmediatamente
    /// en el servidor cuando llega el RPC — no hay delay de frame.
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestUseAbility(int index, Vector3 direction, float attackYaw, RpcInfo info = default)
    {
        if (index < 0 || index >= abilities.Length) return;
        if (states.Get(index) != AbilityState.Ready) return;

        var progression = GetComponent<PlayerProgressionVisuals>();
        if (progression != null && !progression.IsAbilityUnlocked(index)) return;

        var ability = abilities[index];
        if (ability == null) { Debug.LogError("Ability null"); return; }

        Debug.Log($"[SERVER] Player {info.Source} usa skill {index}");

        var netController = GetComponent<NetCharacterController>();
        if (netController != null && attackYaw != 0f)
            netController.SetAttackYaw(attackYaw);

        if (ability is ProjectileAbility proj)
        {
            ProjectileRuntime.Execute(proj, Runner, Object, direction);
        }
        else if (ability is UtilityAbilityData util)
        {
            // Curación: ejecuta inmediatamente en el servidor igual que proyectiles.
            // Luego notifica al NetCharacterAnimator para que escriba NetHealTick
            // con StateAuthority — esto garantiza que los proxies detecten el cambio
            // en Render() y disparen la animación de curación correctamente.
            UtilityRuntime.Execute(util, Runner, Object);
            GetComponent<NetCharacterAnimator>()?.TriggerHealAnimation();
        }

        if (ability.activeTime > 0f)
        {
            states.Set(index, AbilityState.Active);
            activeTimers.Set(index, ability.activeTime);
        }
        else
        {
            states.Set(index, AbilityState.Cooldown);
            cooldowns.Set(index, ability.cooldownTime);
        }
    }

    /// <summary>
    /// Ejecución directa con StateAuthority.
    /// Usado por los enemigos desde EnemyAI.FireProjectile().
    /// </summary>
    public void ExecuteAbilityAuthority(int index, Vector3 direction)
    {
        if (states.Get(index) == AbilityState.Cooldown) return;

        var progression = GetComponent<PlayerProgressionVisuals>();
        if (progression != null && !progression.IsAbilityUnlocked(index)) return;

        var ability = abilities[index];
        if (ability == null) return;

        if (ability is ProjectileAbility proj)
            ProjectileRuntime.Execute(proj, Runner, Object, direction);
        else if (ability is UtilityAbilityData util)
            UtilityRuntime.Execute(util, Runner, Object);

        if (ability.activeTime > 0f)
        {
            states.Set(index, AbilityState.Active);
            activeTimers.Set(index, ability.activeTime);
        }
        else
        {
            states.Set(index, AbilityState.Cooldown);
            cooldowns.Set(index, ability.cooldownTime);
        }
    }

    public bool IsReady(int index)
    {
        if (index < 0 || index >= abilities.Length) return false;
        return states.Get(index) == AbilityState.Ready;
    }

    public float GetCooldownTime(int index)
    {
        if (index < 0 || index >= abilities.Length) return 0f;
        return abilities[index].cooldownTime;
    }

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
        fake.Initialize(ownerId, direction, ability.projectileSpeed, ability.projectileLifetime);
    }
}

enum AbilityState
{
    Ready,
    Active,
    Cooldown
}