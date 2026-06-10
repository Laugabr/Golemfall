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

    /// <summary>
    /// Referencia al animador networked del personaje.
    /// Se usa para disparar la animación de recibir daño en todos los peers.
    /// </summary>
    [SerializeField] private NetCharacterAnimator netAnimator;

    [Networked, OnChangedRender(nameof(OnIsDeadChanged))]
    public NetworkBool IsDead { get; private set; }

    [Networked] public NetworkBool NeedsRespawn { get; set; }

    [SerializeField] public Vector3 _lastSpawnPoint;
    private SimpleKCC simplekcc;

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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            TakeDamage(999, gameObject);
    }

    public void SetLastSpawnPoint(Vector3 position)
    {
        if (!Object.HasStateAuthority) return;
        _lastSpawnPoint = position;
        Debug.Log($"[SERVER] LastSpawnPoint seteado en {position}");
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

    public override void FixedUpdateNetwork() { }

    public override void Die()
    {
        if (!Object.HasStateAuthority || IsDead) return;

        Debug.Log($"[SERVER] {gameObject.name} murió.");
        IsDead = true;

        if (Object.HasStateAuthority)
            StartCoroutine(RespawnRoutine());
    }

    /// <summary>
    /// Se llama via OnChangedRender cada vez que CurrentHealth cambia.
    /// Dispara la animación de recibir daño si el jugador está vivo y perdió vida.
    /// Solo aplica cuando el daño viene del servidor (StateAuthority).
    /// </summary>
    public override void CurrentHealthChanged()
    {
        base.CurrentHealthChanged();

        // Solo dispara si está vivo y recibió daño (no al curarse ni al spawnar)
        if (!IsDead && CurrentHealth < MaxHealth && CurrentHealth > 0)
            netAnimator?.TriggerTakeDamage();
    }

    private void OnIsDeadChanged()
    {
        if (IsDead)
        {
            playerCollider.enabled = false;
            var kcc = GetComponent<Fusion.Addons.SimpleKCC.SimpleKCC>();
            if (kcc != null) kcc.SetGravity(0f);
        }
        else
        {
            SetAlive(true);
            var kcc = GetComponent<Fusion.Addons.SimpleKCC.SimpleKCC>();
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