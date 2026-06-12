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
/// </summary>
public class AbilityHolder : NetworkBehaviour
{
    [SerializeField] private Ability[] abilities;
    [SerializeField] private GameObject[] fakePrefabs; // mismo orden que abilities[]

    // Arrays networked para sincronizar el estado de cada habilidad a todos los peers
    [Networked, Capacity(4)] private NetworkArray<float> cooldowns => default;
    [Networked, Capacity(4)] private NetworkArray<float> activeTimers => default;
    [Networked, Capacity(4)] private NetworkArray<AbilityState> states => default;

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
                    // Descuenta el tiempo activo y pasa a cooldown cuando termina
                    float active = activeTimers[i] - Runner.DeltaTime;
                    activeTimers.Set(i, active);
                    if (active <= 0)
                    {
                        states.Set(i, AbilityState.Cooldown);
                        cooldowns.Set(i, abilities[i].cooldownTime);
                    }
                    break;

                case AbilityState.Cooldown:
                    // Descuenta el cooldown y pasa a Ready cuando termina
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
    /// El servidor ejecuta directo. El cliente manda un RPC y spawna
    /// un proyectil visual falso para feedback inmediato.
    /// NO usar para enemigos, usar ExecuteAbilityAuthority() en su lugar.
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
            // Feedback visual inmediato en el cliente mientras espera confirmación del servidor
            SpawnFakeProjectile(index, direction);
            RPC_RequestUseAbility(index, direction);
        }
    }

    /// <summary>
    /// RPC del cliente al servidor para validar y ejecutar la habilidad.
    /// El servidor rechaza si la habilidad no está en estado Ready.
    /// Bloquea durante Active Y Cooldown para evitar spam.
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestUseAbility(int index, Vector3 direction, RpcInfo info = default)
    {
        if (index < 0 || index >= abilities.Length) return;

        if (states.Get(index) != AbilityState.Ready)
        {
            Debug.Log($"[SERVER] Skill {index} en cooldown");
            return;
        }

        var ability = abilities[index];
        if (ability == null) { Debug.LogError("Ability null"); return; }

        Debug.Log($"[SERVER] Player {info.Source} usa skill {index}");

        if (ability is ProjectileAbility proj)
            ProjectileRuntime.Execute(proj, Runner, Object, direction);

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
    /// Usado por los enemigos desde EnemyAI.FireProjectile() ya que los enemigos
    /// tienen StateAuthority pero nunca InputAuthority.
    /// También usado internamente por TryUseAbility() cuando corre en el servidor.
    /// </summary>
    public void ExecuteAbilityAuthority(int index, Vector3 direction)
    {
        if (states.Get(index) == AbilityState.Cooldown) return;

        var ability = abilities[index];
        if (ability == null) return;

        if (ability is ProjectileAbility proj)
            ProjectileRuntime.Execute(proj, Runner, Object, direction);

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
    /// Devuelve true si la habilidad está lista para usarse.
    /// Usado por el NetCharacterAnimator para bloquear el trigger
    /// de animación cuando la habilidad está en cooldown.
    /// </summary>
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

    /// <summary>
    /// Spawna un proyectil visual local sin red para feedback inmediato en el cliente.
    /// Se destruye cuando llega el proyectil real del servidor.
    /// </summary>
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

/// <summary>
/// Estados posibles de una habilidad.
/// Ready: disponible para usar.
/// Active: ejecutándose, esperando que termine el activeTime.
/// Cooldown: en espera, no se puede usar hasta que termine el cooldownTime.
/// </summary>
enum AbilityState
{
    Ready,
    Active,
    Cooldown
}