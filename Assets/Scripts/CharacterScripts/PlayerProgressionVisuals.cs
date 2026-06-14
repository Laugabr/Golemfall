using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[Serializable]
public class LevelUnlock
{
    public int requiredLevel;

    [Header("Máscaras visuales")]
    public GameObject maskToDisable;
    public GameObject maskToEnable;

    [Header("Habilidades")]
    public int abilityIndex = -1; // -1 = ninguna
}

public class PlayerProgressionVisuals : NetworkBehaviour
{
    [SerializeField] private List<LevelUnlock> unlocks = new();
    [SerializeField] private AbilityHolder abilityHolder;
    [SerializeField] private ExperienceManager expManager;

    // Abilities bloqueadas hasta que se desbloqueen por nivel
    // Se configura en inspector — qué abilities empiezan bloqueadas
    [SerializeField] private List<int> lockedAbilities = new();

    [Networked, OnChangedRender(nameof(OnLevelChanged))]
    private int _renderedLevel { get; set; }

    public override void Spawned()
    {
        abilityHolder = GetComponent<AbilityHolder>();
        expManager = GetComponent<ExperienceManager>();

        // Aplicar estado inicial (nivel 1)
        if (Object.HasStateAuthority)
            _renderedLevel = 1;

        ApplyUnlocksUpToLevel(_renderedLevel);
    }

    // Llamado desde ExperienceManager cuando sube de nivel
    public void OnPlayerLevelUp(int newLevel)
    {
        if (!Object.HasStateAuthority) return;
        _renderedLevel = newLevel;
    }

    private void OnLevelChanged()
    {
        ApplyUnlocksUpToLevel(_renderedLevel);
    }

    private void ApplyUnlocksUpToLevel(int level)
    {
        // Solo el owner ve sus propios visuales
        if (!Object.HasInputAuthority) return;

        foreach (var unlock in unlocks)
        {
            if (level < unlock.requiredLevel) continue;

            // Máscaras
            if (unlock.maskToDisable != null)
                unlock.maskToDisable.SetActive(false);
            if (unlock.maskToEnable != null)
                unlock.maskToEnable.SetActive(true);

            // Desbloquear habilidad
            if (unlock.abilityIndex >= 0)
                lockedAbilities.Remove(unlock.abilityIndex);
        }
    }

    // Llamado desde NetCharacterController antes de usar una habilidad
    public bool IsAbilityUnlocked(int index) => !lockedAbilities.Contains(index);
}