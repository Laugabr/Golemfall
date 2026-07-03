using Fusion;
using Fusion.Addons.SimpleKCC;
using Game.CameraSystem;
using System.Collections;
using UnityEngine;

public class PlayerHealth : HealthSystem
{
    [SerializeField] private float timeToRespawn = 5f;
    [SerializeField] private GameObject bodyVisualsGO;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private NetCharacterAnimator netAnimator;

    [Networked, OnChangedRender(nameof(OnIsDeadChanged))]
    public NetworkBool IsDead { get; private set; }

    [Networked] public NetworkBool NeedsRespawn { get; set; }

    [SerializeField] public Vector3 _lastSpawnPoint;
    private SimpleKCC simplekcc;

    [Header("Arena")]
    [Tooltip("Mientras está en true, Die() NO dispara el respawn individual " +
             "automático: el player se queda muerto/desactivado. Lo prende/apaga " +
             "el ArenaRespawnManager al activar/desactivar la pelea contra el boss.")]
    [SerializeField] private bool arenaFightActive = false;

    public bool IsArenaFightActive => arenaFightActive;

    /// <summary>
    /// Llamado por ArenaRespawnManager al activar/desactivar la pelea.
    /// Mientras esté en true, las muertes no auto-respawnean (ver Die()).
    /// </summary>
    public void SetArenaFightActive(bool active)
    {
        arenaFightActive = active;
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(1.625f);
        yield return new WaitForSeconds(2f);

        if (bodyVisualsGO != null)
            bodyVisualsGO.SetActive(false);

        CurrentHealth = MaxHealth;
        IsDead = false;

        NeedsRespawn = true;
    }

    public override void Spawned()
    {
        base.Spawned();

        playerCollider = GetComponent<Collider>();

        if (Object.HasStateAuthority)
        {
            if (stats is PlayerStats playerStats)
                playerStats.OnStatsChanged.AddListener(RecalculateMaxHealth);

            RecalculateMaxHealth();
        }

        _lastSpawnPoint = transform.position;

        simplekcc = GetComponent<SimpleKCC>();
        if (simplekcc == null)
            Debug.LogError($"PlayerHealth requires SimpleKCC on {gameObject.name}");

        SetAlive(true);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!NeedsRespawn) return;

        NeedsRespawn = false;

        simplekcc.SetPosition(_lastSpawnPoint);

        bodyVisualsGO?.SetActive(true);
        playerCollider.enabled = true;

    }

    public void SetLastSpawnPoint(Vector3 position)
    {
        if (!Object.HasStateAuthority) return;
        _lastSpawnPoint = position;
        //Debug.Log($"[SERVER] LastSpawnPoint seteado en {position}");
    }

    private void OnDestroy()
    {
        if (stats is PlayerStats playerStats)
            playerStats.OnStatsChanged.RemoveListener(RecalculateMaxHealth);
    }

    public void RecalculateMaxHealth()
    {
        if (!Object.HasStateAuthority) return;

        int newMax = stats.GetStat(Stat.maxHealth);
        MaxHealth = newMax;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
    }

    public override int GetArmor() => stats.GetStat(Stat.armor);

    public override void Die()
    {
        if (!Object.HasStateAuthority || IsDead) return;

        //Debug.Log($"[SERVER] {gameObject.name} murió.");
        IsDead = true;

        // En pelea de arena: NO se respawnea solo. Se queda muerto/desactivado
        // (lo maneja OnIsDeadChanged) hasta que ArenaRespawnManager detecte que
        // todos murieron y llame ForceRespawn() en cada uno.
        if (arenaFightActive)
        {
            //Debug.Log($"[SERVER] {gameObject.name} queda muerto esperando wipe de arena");
            return;
        }

        StartCoroutine(RespawnRoutine());
    }

    /// <summary>
    /// Llamado por ArenaRespawnManager cuando detecta que TODOS los players
    /// murieron durante la pelea (wipe). Respawnea a este player de inmediato
    /// en spawnPoint, sin pasar por el delay de RespawnRoutine() (ese timing
    ////efecto de wipe ya lo maneja el manager).
    /// </summary>
    public void ForceRespawn(Vector3 spawnPoint)
    {
        if (!Object.HasStateAuthority) return;

        StopAllCoroutines(); // por si había una RespawnRoutine vieja colgada

        _lastSpawnPoint = spawnPoint;

        CurrentHealth = MaxHealth;
        IsDead = false;
        NeedsRespawn = true;
    }
    /// <summary>
    /// Teletransporta a un player VIVO a un punto, sin tocar IsDead/CurrentHealth.
    /// Pensado para el teleport grupal al entrar a la arena (ArenaRespawnManager.
    /// TeleportPlayersIntoArena), donde el player no murió, solo hay que moverlo.
    /// Distinto de ForceRespawn(), que sí resetea vida/estado porque asume que
    /// el player venía muerto esperando el wipe.
    /// </summary>
    public void TeleportTo(Vector3 point)
    {
        if (!Object.HasStateAuthority) return;

        _lastSpawnPoint = point;
        NeedsRespawn = true;
    }
    public override void CurrentHealthChanged()
    {
        base.CurrentHealthChanged();

        if (!IsDead && CurrentHealth < MaxHealth && CurrentHealth > 0)
        {
            netAnimator?.TriggerTakeDamage();

            if (Object.HasInputAuthority)
                CameraController.Local?.Shake(0.15f, 0.25f);
        }
    }

    private void OnIsDeadChanged()
    {
        if (IsDead)
        {
            playerCollider.enabled = false;
            var kcc = GetComponent<SimpleKCC>();
            if (kcc != null) kcc.SetGravity(0f);

            // Desactivamos visuals acá también (antes solo lo hacía
            // RespawnRoutine() al final del timer, pero ahora en arena
            // el player puede quedarse muerto indefinidamente).
            if (bodyVisualsGO != null)
                bodyVisualsGO.SetActive(false);
        }
        else
        {
            SetAlive(true);
            var kcc = GetComponent<SimpleKCC>();
            if (kcc != null) kcc.SetGravity(Physics.gravity.y * 3f);

            if (Object.HasInputAuthority)
            {
                var cam = Camera.main?.GetComponent<CameraController>();
                if (cam != null) cam.SnapToTarget();
            }
        }
    }

    private void SetAlive(bool alive)
    {
        bodyVisualsGO.SetActive(alive);
        playerCollider.enabled = alive;
    }

    public override void MaxHealthChanged() =>
        Debug.Log($"MaxHealth → {MaxHealth}");
}