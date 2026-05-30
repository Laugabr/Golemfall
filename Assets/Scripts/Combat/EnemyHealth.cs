using UnityEngine;

public class EnemyHealth : HealthSystem
{
    [SerializeField] private EnemyHealthBar healthBar;

    // Referencias para disparar animaciones y desactivar la AI en la muerte.
    // Se auto-resuelven en Spawned() si no se asignan por inspector.
    [SerializeField] private NetEnemyAnimator netAnimator;
    [SerializeField] private EnemyAI enemyAI;

    [Header("Death")]
    [Tooltip("Segundos que esperamos después de Die() antes de hacer Despawn, " +
             "para dar tiempo a que se reproduzca la animación de muerte.")]
    [SerializeField] private float deathAnimDuration = 1.5f;

    // Cache local del último valor observado de CurrentHealth. Lo usamos en
    // CurrentHealthChanged() para detectar disminuciones (= recibió daño) sin
    // necesidad de un campo networked extra. No se replica: cada peer mantiene
    // su propio cache local.
    private int _lastObservedHealth = -1;

    // Guard contra dobles llamadas a Die() en el mismo frame (ej: dos hits
    // simultáneos en el mismo tick). Evita doble Invoke del Despawn.
    private bool _dieScheduled;

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
        }

        // Auto-resolve si no se asignaron por inspector.
        if (netAnimator == null) netAnimator = GetComponent<NetEnemyAnimator>();
        if (enemyAI == null) enemyAI = GetComponent<EnemyAI>();

        // Baseline para detectar "disminuyó la vida" en el primer cambio que
        // observemos en este peer. Sin esto, el primer CurrentHealthChanged
        // podría disparar take-damage incorrectamente si _lastObservedHealth
        // arrancaba en 0.
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

        // Detección de daño piggybackeada sobre el [OnChangedRender] de CurrentHealth.
        //   - Fire en TODOS los peers (host + proxies) automáticamente.
        //   - newHealth > 0  → no disparamos take-damage en el golpe mortal;
        //                      esa animación la maneja deathTrigger via SetDead().
        //   - _lastObservedHealth > 0 → evita falsos positivos en el primer
        //                                snapshot recibido por un proxy.
        if (_lastObservedHealth > 0 && newHealth < _lastObservedHealth && newHealth > 0)
        {
            netAnimator?.TriggerTakeDamage();
        }

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

    public override void Die()
    {
        Debug.Log($"Enemy {gameObject.name} murió");

        if (!Object.HasStateAuthority) return;
        if (_dieScheduled) return; // guard contra dobles Die() en el mismo tick
        _dieScheduled = true;

        // 1) Replicar estado "muerto" a todos los peers — esto dispara
        //    deathTrigger en cada uno vía OnIsDeadChanged en el animator.
        netAnimator?.SetDead();

        // 2) Frenar la AI y el agent: la BT no debería seguir mientras se
        //    reproduce la animación de muerte (atacaría desde la pose de muerto).
        enemyAI?.DisableAI();

        // 3) Diferir el despawn para que se vea la animación. Si el NetworkObject
        //    se destruye antes (escena cerrada, runner detenido, etc.), Invoke
        //    se cancela automáticamente porque el MonoBehaviour deja de existir.
        Invoke(nameof(DoDespawn), deathAnimDuration);
    }

    private void DoDespawn()
    {
        // Doble check de validez por si el objeto fue invalidado entre el Invoke
        // y la ejecución (cierre de sesión, host migration, etc.).
        if (Object != null && Object.IsValid && Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}