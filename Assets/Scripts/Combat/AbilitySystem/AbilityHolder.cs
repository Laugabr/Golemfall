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
///   - El RPC valida, activa el cooldown y guarda los datos de curación.
///   - El efecto real (spawn del UtilityAbility) se dispara en el frame 11
///     de la animación via HealAtFrame.cs → ExecuteHealEffect().
///   - Solo el StateAuthority ejecuta el spawn.
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

    // Datos de curación pendiente — guardados por el RPC y ejecutados
    // en el frame 11 de la animación via HealAtFrame.cs
    private bool _healPending;
    private UtilityAbilityData _pendingHealData;

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
            SpawnFakeProjectile(index, direction);
            RPC_RequestUseAbility(index, direction, 0f);
        }
    }

    /// <summary>
    /// RPC del cliente al servidor para validar y ejecutar la habilidad.
    /// El servidor rechaza si la habilidad no está en estado Ready.
    ///
    /// Para la habilidad de curación (UtilityAbilityData), el RPC solo
    /// activa el cooldown y guarda los datos — el spawn real ocurre en
    /// el frame 11 de la animación via HealAtFrame → ExecuteHealEffect().
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

        // Escribimos el yaw de ataque ANTES de cambiar el estado de la habilidad.
        // Si lo hiciéramos después, IsReady ya devolvería false y el
        // NetCharacterController no podría escribir NetBodyYaw desde FUN.
        var netController = GetComponent<NetCharacterController>();
        if (netController != null && attackYaw != 0f)
            netController.SetAttackYaw(attackYaw);

        if (ability is ProjectileAbility proj)
        {
            // Proyectiles: ejecutan inmediatamente
            ProjectileRuntime.Execute(proj, Runner, Object, direction);
        }
        else if (ability is UtilityAbilityData util)
        {
            // Curación: guardamos los datos y esperamos al frame 11 de la animación.
            // HealAtFrame.cs llama ExecuteHealEffect() cuando llega al frame correcto.
            _healPending = true;
            _pendingHealData = util;
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
    /// Llamado por HealAtFrame en el frame 11 de la animación HealAbility.
    /// Solo ejecuta en el StateAuthority — spawna el UtilityAbility con los
    /// datos guardados por el RPC.
    /// </summary>
    public void ExecuteHealEffect()
    {
        if (!Object.HasStateAuthority) return;
        if (!_healPending || _pendingHealData == null) return;

        UtilityRuntime.Execute(_pendingHealData, Runner, Object);

        _healPending = false;
        _pendingHealData = null;
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
    /// </summary>
    public bool IsReady(int index)
    {
        if (index < 0 || index >= abilities.Length) return false;
        return states.Get(index) == AbilityState.Ready;
    }

    /// <summary>
    /// Devuelve el cooldown time de la habilidad desde el ScriptableObject.
    /// Usado por NetCharacterAnimator para calcular el cooldown en ticks sin
    /// depender de IsReady, que puede estar desactualizado en el mismo tick
    /// en que se ejecuta el RPC.
    /// </summary>
    public float GetCooldownTime(int index)
    {
        if (index < 0 || index >= abilities.Length) return 0f;
        return abilities[index].cooldownTime;
    }

    /// <summary>
    /// Spawna un proyectil visual local sin red para feedback inmediato en el cliente.
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

    public void SpawnFakeProjectilePublic(int index, Vector3 direction)
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