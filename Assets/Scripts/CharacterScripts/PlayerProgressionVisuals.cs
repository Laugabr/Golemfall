using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Define un desbloqueo que ocurre al alcanzar un nivel determinado.
/// Cada entrada puede desbloquear una habilidad, el dash, o cambiar
/// una máscara visual — de forma independiente para mayor flexibilidad.
/// </summary>
[Serializable]
public class LevelUnlock
{
    [Tooltip("Nombre descriptivo para identificar este desbloqueo en el Inspector.")]
    public string displayName = "Nuevo desbloqueo";

    [Tooltip("Nivel mínimo requerido para que este desbloqueo se active.")]
    public int requiredLevel = 1;

    [Header("Habilidad")]
    [Tooltip("Índice de la habilidad en AbilityHolder que se desbloquea.\n" +
             "-1 = ninguna\n" +
             "0  = Melee (siempre disponible, no usar acá)\n" +
             "1  = Rango\n" +
             "2  = Curación")]
    public int abilityIndex = -1;

    [Header("Dash")]
    [Tooltip("Si true, desbloquea el dash al alcanzar el nivel requerido.")]
    public bool unlockDash = false;

    [Header("Máscaras visuales")]
    [Tooltip("Máscara que se desactiva al alcanzar el nivel. Dejar vacío si no aplica.")]
    public GameObject maskToDisable;

    [Tooltip("Máscara que se activa al alcanzar el nivel. Dejar vacío si no aplica.")]
    public GameObject maskToEnable;
}

/// <summary>
/// Maneja el desbloqueo progresivo de habilidades, dash y visuales del personaje.
///
/// Flujo:
///   - Al spawnear inicializa todo como bloqueado según la configuración del Inspector.
///   - Aplica los desbloqueos hasta el nivel actual al spawnear.
///   - Cuando sube de nivel, aplica los desbloqueos nuevos via OnLevelChanged.
///   - NetCharacterController y NetCharacterAnimator consultan IsAbilityUnlocked()
///     e IsDashUnlocked() antes de ejecutar o animar cada habilidad.
///
/// Ejemplo de configuración en el Inspector:
///   Element 0 — "Dash"       requiredLevel=2  unlockDash=true
///   Element 1 — "Rango"      requiredLevel=3  abilityIndex=1
///   Element 2 — "Máscara 2"  requiredLevel=3  maskToDisable=owl01  maskToEnable=owl02
///   Element 3 — "Curación"   requiredLevel=5  abilityIndex=2
///   Element 4 — "Máscara 3"  requiredLevel=5  maskToDisable=owl02  maskToEnable=owl03
/// </summary>
public class PlayerProgressionVisuals : NetworkBehaviour
{
    [SerializeField] private List<LevelUnlock> unlocks = new();
    [SerializeField] private AbilityHolder abilityHolder;
    [SerializeField] private ExperienceManager expManager;

    // Habilidades bloqueadas por índice — se van removiendo al desbloquear por nivel.
    // Se inicializa en Spawned() leyendo la configuración del Inspector.
    private List<int> _lockedAbilities = new();

    // El dash empieza bloqueado si hay algún unlock de dash configurado.
    private bool _dashLocked;

    [Networked, OnChangedRender(nameof(OnLevelChanged))]
    private int _renderedLevel { get; set; }

    public override void Spawned()
    {
        abilityHolder = GetComponent<AbilityHolder>();
        expManager = GetComponent<ExperienceManager>();

        // Inicializamos todo como bloqueado según lo configurado en el Inspector.
        // Solo las habilidades y el dash que tengan un unlock definido arrancan bloqueados.
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

        // Usamos max(1, _renderedLevel) para que el cliente también
        // aplique el nivel mínimo aunque _renderedLevel llegue tarde del host.
        ApplyUnlocksUpToLevel(Mathf.Max(1, _renderedLevel));
    }

    /// <summary>
    /// Llamado desde ExperienceManager cuando el jugador sube de nivel.
    /// Solo el StateAuthority puede escribir _renderedLevel — esto dispara
    /// OnLevelChanged en todos los peers via OnChangedRender.
    /// </summary>
    public void OnPlayerLevelUp(int newLevel)
    {
        if (!Object.HasStateAuthority) return;
        _renderedLevel = newLevel;
    }

    private void OnLevelChanged()
    {
        ApplyUnlocksUpToLevel(_renderedLevel);
    }

    /// <summary>
    /// Aplica todos los desbloqueos cuyo nivel requerido sea menor o igual al nivel dado.
    /// Solo el owner local aplica los cambios visuales — las máscaras son puramente
    /// visuales y no necesitan sincronización en red.
    /// </summary>
    private void ApplyUnlocksUpToLevel(int level)
    {
        if (!Object.HasInputAuthority) return;

        foreach (var unlock in unlocks)
        {
            if (level < unlock.requiredLevel) continue;

            // Máscaras visuales
            if (unlock.maskToDisable != null)
                unlock.maskToDisable.SetActive(false);
            if (unlock.maskToEnable != null)
                unlock.maskToEnable.SetActive(true);

            // Desbloquear habilidad por índice
            if (unlock.abilityIndex >= 0)
                _lockedAbilities.Remove(unlock.abilityIndex);

            // Desbloquear dash
            if (unlock.unlockDash)
                _dashLocked = false;
        }
    }

    /// <summary>
    /// Devuelve true si la habilidad con ese índice está desbloqueada.
    /// Consultado por NetCharacterController y NetCharacterAnimator antes
    /// de ejecutar o animar la habilidad.
    /// </summary>
    public bool IsAbilityUnlocked(int index) => !_lockedAbilities.Contains(index);

    /// <summary>
    /// Devuelve true si el dash está desbloqueado.
    /// Consultado por NetCharacterController antes de ejecutar el dash
    /// y por NetCharacterAnimator antes de animar el dash.
    /// </summary>
    public bool IsDashUnlocked() => !_dashLocked;
}