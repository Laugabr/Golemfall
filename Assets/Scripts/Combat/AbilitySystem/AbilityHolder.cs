using Fusion;
using UnityEngine;
using Golemfall.Abilities;

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
///   - El RPC valida, activa el cooldown y guarda los datos en _healPending.
///   - El servidor dispara la animación via TriggerHealAnimation() en NetCharacterAnimator.
///   - HealAtFrame detecta el frame 11 en el servidor y llama ExecuteHealEffect().
///   - El VFX se manda via NetworkVFXManager.RPC_SpawnHealVFX().
///   - Este patrón garantiza que la curación y el VFX coincidan con la pose máxima
///     sin depender de cálculos de ticks — basta con cambiar targetFrame en el Inspector.
/// </summary>
public class AbilityHolder : NetworkBehaviour
{
    [SerializeField] private Ability[] abilities;
    [SerializeField] private GameObject[] fakePrefabs;

    [Networked, Capacity(4)] private NetworkArray<float> cooldowns => default;
    [Networked, Capacity(4)] private NetworkArray<float> activeTimers => default;
    [Networked, Capacity(4)] private NetworkArray<AbilityState> states => default;

    private PlayerProgressionVisuals _progression;

    // Datos de curación pendiente — guardados por el RPC y ejecutados
    // en el frame 11 via HealAtFrame.cs → ExecuteHealEffect()
    private bool _healPending;
    private UtilityAbilityData _pendingHealData;

    public override void Spawned()
    {
        _progression = GetComponent<PlayerProgressionVisuals>();
    }

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
    /// Para la curación: guarda los datos y dispara la animación en el servidor.
    /// HealAtFrame se encarga de ejecutar el efecto en el frame correcto.
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
            // Guardamos los datos y disparamos la animación en el servidor.
            // HealAtFrame detecta el frame 11 y llama ExecuteHealEffect().
            // Como TriggerHealAnimation() dispara la animación en el mismo tick
            // que llega el RPC, el timing entre animación y frame 11 es perfecto.
            _healPending = true;
            _pendingHealData = util;
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
    /// Llamado por HealAtFrame en el frame configurado de la animación HealAbility.
    /// Solo ejecuta en StateAuthority — spawna el UtilityAbility con los datos
    /// guardados por el RPC.
    /// </summary>
    public void ExecuteHealEffect()
    {
        if (!Object.HasStateAuthority) return;
        if (!_healPending || _pendingHealData == null) return;

        UtilityRuntime.Execute(_pendingHealData, Runner, Object);

        _healPending = false;
        _pendingHealData = null;
    }

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

