using UnityEngine;

public class EnemyHealth : HealthSystem
{
    [SerializeField] private EnemyHealthBar healthBar;
    [SerializeField] private NetEnemyAnimator netAnimator;
    [SerializeField] private EnemyAI enemyAI;

    [Header("Death")]
    [SerializeField] private float deathAnimDuration = 1.5f;

    private int _lastObservedHealth = -1;
    private bool _dieScheduled;
    private GameObject _lastDamageSource;

    public override void Spawned()
    {
        var charStats = GetComponent<CharacterStats>();
        if (charStats != null)
            charStats.Initialize();

        base.Spawned();

        if (Object.HasStateAuthority)
        {
            RecalculateMaxHealth();
            CurrentHealth = MaxHealth;
            healthBar?.SetMaxHealth(MaxHealth);
            healthBar?.SetHealth(CurrentHealth);
            Debug.Log($"[EnemySpawn] {gameObject.name} en pos {transform.position}");
        }

        if (netAnimator == null) netAnimator = GetComponent<NetEnemyAnimator>();
        if (enemyAI == null) enemyAI = GetComponent<EnemyAI>();

        _lastObservedHealth = CurrentHealth;
    }

    public override void Render()
    {
        healthBar?.SetMaxHealth(MaxHealth);
        healthBar?.SetHealth(CurrentHealth);
    }

    public override void CurrentHealthChanged()
    {
        base.CurrentHealthChanged();
        healthBar?.SetHealth(CurrentHealth);

        int newHealth = CurrentHealth;

        if (_lastObservedHealth > 0 && newHealth < _lastObservedHealth && newHealth > 0)
            netAnimator?.TriggerTakeDamage();

        _lastObservedHealth = newHealth;
    }

    public override void MaxHealthChanged()
    {
        base.MaxHealthChanged();
        healthBar?.SetMaxHealth(MaxHealth);
        healthBar?.SetHealth(CurrentHealth);
    }

    private void RecalculateMaxHealth()
    {
        if (!Object.HasStateAuthority) return;

        int newMax = stats.GetStat(Stat.maxHealth);
        MaxHealth = newMax;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
    }

    public override int GetArmor()
    {
        if (stats != null)
            return stats.GetStat(Stat.armor);
        return 0;
    }

    public override void TakeDamage(int amount, GameObject source)
    {
        // Guardamos el origen antes de que base pueda disparar Die()
        _lastDamageSource = source;
        base.TakeDamage(amount, source);
    }

    public override void Die()
    {
        Debug.Log($"Enemy {gameObject.name} murió");

        if (!Object.HasStateAuthority) return;
        if (_dieScheduled) return;
        _dieScheduled = true;

        // Solo cuenta como kill si el golpe letal vino de un jugador.
        // Muertes ambientales (agua, caídas) igual despawnean, pero no cuentan.
        bool killedByPlayer = _lastDamageSource != null && _lastDamageSource.CompareTag("Player");
        if (killedByPlayer)
        {
            TrackEvents.OnTrackEvent?.Invoke(GameEventType.KillEnemy, 1);
            ExperienceManager.GrantKillXpToAll();   // XP grupal a todos los jugadores
        }

        netAnimator?.SetDead();
        enemyAI?.DisableAI();

        Invoke(nameof(DoDespawn), deathAnimDuration);
    }

    private void DoDespawn()
    {
        if (Object != null && Object.IsValid && Object.HasStateAuthority)
            Runner.Despawn(Object);
    }
}