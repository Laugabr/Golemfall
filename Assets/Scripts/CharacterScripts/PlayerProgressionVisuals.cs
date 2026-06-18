using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Configuración de un desbloqueo por nivel — editable desde el Inspector.
/// Cada entrada es independiente: puede desbloquear una habilidad, el dash,
/// o cambiar una máscara visual, o cualquier combinación.
/// </summary>
[Serializable]
public class LevelUnlock
{
    [Tooltip("Nombre descriptivo para el Inspector.")]
    public string displayName = "Nuevo desbloqueo";

    [Tooltip("Nivel mínimo para activar este desbloqueo.")]
    public int requiredLevel = 1;

    [Header("Habilidad")]
    [Tooltip("-1 = ninguna | 1 = Rango | 2 = Curación")]
    public int abilityIndex = -1;

    [Header("Dash")]
    [Tooltip("Si true, desbloquea el dash al alcanzar el nivel.")]
    public bool unlockDash = false;

    [Header("Máscaras visuales")]
    public GameObject maskToDisable;
    public GameObject maskToEnable;
}

/// <summary>
/// Maneja el desbloqueo progresivo de habilidades, dash y máscaras del personaje.
///
/// DISEÑO:
///   - _lockedAbilities y _dashLocked son variables LOCALES (no networked).
///     Cada peer las inicializa en Spawned() y las actualiza al subir de nivel.
///   - El nivel actual viene de _renderedLevel que SÍ es [Networked] y se
///     sincroniza via OnChangedRender cuando el host escribe el nuevo nivel.
///   - ApplyUnlocksUpToLevel corre en TODOS los peers para que las máscaras
///     visuales se vean correctamente en todas las máquinas.
///
/// Configuración recomendada en el Inspector:
///   Element 0 — "Dash"       requiredLevel=2  unlockDash=true
///   Element 1 — "Rango"      requiredLevel=3  abilityIndex=1
///   Element 2 — "Máscara 2"  requiredLevel=3  maskToEnable=obj_maskOwl.02  maskToDisable=obj_maskOwl.01
///   Element 3 — "Curación"   requiredLevel=5  abilityIndex=2
///   Element 4 — "Máscara 3"  requiredLevel=5  maskToEnable=obj_maskOwl.03  maskToDisable=obj_maskOwl.02
/// </summary>
public class PlayerProgressionVisuals : NetworkBehaviour
{
    [SerializeField] private List<LevelUnlock> unlocks = new();

    // Lista local de habilidades bloqueadas — inicializada en Spawned()
    // según la configuración del Inspector.
    private readonly List<int> _lockedAbilities = new();
    private bool _dashLocked;

    /// <summary>
    /// Nivel actual del jugador, sincronizado en red.
    /// El host lo escribe via OnPlayerLevelUp().
    /// Todos los peers aplican los desbloqueos cuando cambia via OnChangedRender.
    /// </summary>
    [Networked, OnChangedRender(nameof(OnLevelChanged))]
    private int _renderedLevel { get; set; }
    [Header("Level Up VFX")]
    [SerializeField] private ParticleSystem levelUpVFX;

    
    public override void Spawned()
    {
        // Inicializamos qué está bloqueado según la configuración del Inspector.
        _lockedAbilities.Clear();
        _dashLocked = false;

        foreach (var unlock in unlocks)
        {
            if (unlock.abilityIndex >= 0 && !_lockedAbilities.Contains(unlock.abilityIndex))
                _lockedAbilities.Add(unlock.abilityIndex);

            if (unlock.unlockDash)
                _dashLocked = true;
        }

        if (Object.HasStateAuthority)
            _renderedLevel = 1;

        // Usamos Max(1, _renderedLevel) para que el cliente aplique el nivel mínimo
        // aunque el snapshot todavía no llegó del host.
        ApplyUnlocksUpToLevel(Mathf.Max(1, _renderedLevel));
    }

    /// <summary>
    /// Llamado desde ExperienceManager cuando el jugador sube de nivel.
    /// Solo el host puede escribir — OnChangedRender dispara OnLevelChanged
    /// en todos los peers automáticamente.
    /// </summary>
    public void OnPlayerLevelUp(int newLevel)
    {
        if (!Object.HasStateAuthority) return;
        _renderedLevel = newLevel;

        RPC_PlayLevelUpVFX();

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayLevelUpVFX()
    {
        // Todos los peers ven el VFX del jugador que subió de nivel
        // porque el RPC corre en el objeto de ESE jugador específico
        levelUpVFX?.Play();
    }

    private void OnLevelChanged()
    {
        ApplyUnlocksUpToLevel(_renderedLevel);
    }

    /// <summary>
    /// Aplica todos los desbloqueos cuyo nivel requerido sea <= al nivel dado.
    /// Corre en TODOS los peers — tanto host como cliente necesitan:
    ///   - Ver las máscaras de todos los jugadores correctamente
    ///   - Saber qué habilidades y dash están desbloqueados para validar inputs
    /// </summary>
    private void ApplyUnlocksUpToLevel(int level)
    {
        foreach (var unlock in unlocks)
        {
            if (level < unlock.requiredLevel) continue;

            // Máscaras — todos los peers las aplican para que cada jugador
            // vea las máscaras correctas de todos los demás jugadores.
            if (unlock.maskToDisable != null)
                unlock.maskToDisable.SetActive(false);
            if (unlock.maskToEnable != null)
                unlock.maskToEnable.SetActive(true);

            // Habilidades y dash — todos los peers necesitan saberlo para
            // que AbilityHolder, NetCharacterController y NetCharacterAnimator
            // funcionen correctamente en cada máquina.
            if (unlock.abilityIndex >= 0)
                _lockedAbilities.Remove(unlock.abilityIndex);

            if (unlock.unlockDash)
                _dashLocked = false;
        }
    }

    /// <summary>
    /// Devuelve true si la habilidad está desbloqueada.
    /// Consultado por NetCharacterController, NetCharacterAnimator y AbilityHolder.
    /// </summary>
    public bool IsAbilityUnlocked(int index) => !_lockedAbilities.Contains(index);

    /// <summary>
    /// Devuelve true si el dash está desbloqueado.
    /// Consultado por NetCharacterController y NetCharacterAnimator.
    /// </summary>
    public bool IsDashUnlocked() => !_dashLocked;
}