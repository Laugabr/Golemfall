using Fusion;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Maneja las animaciones networked de los enemigos (melee y ranged).
///
/// DIFERENCIAS CLAVE vs NetCharacterAnimator (player):
///   - El enemigo NO tiene InputAuthority. Solo el host (StateAuthority) lo conduce.
///   - "Owner" para nosotros es siempre el host. Todos los demás peers son proxies.
///   - No hay prediction local — el cliente nunca corre la BT ni decide ataques.
///   - El host es la única fuente de verdad: escribe los [Networked]; los proxies
///     leen en Render() y aplican al Animator.
///
/// PATRONES DE NETWORKING APLICADOS ACÁ:
///
///   1) Bools / ints (isWalking, idleType):
///      SetBool / SetInteger en Render() en TODOS los peers. Son idempotentes,
///      no hace falta filtrar por owner.
///
///   2) Ataque (trigger): "tick stamp" igual que el player.
///      El host escribe NetAttackTick + dispara SetTrigger local en el mismo tick.
///      Los proxies detectan el cambio del tick en Render() y disparan una vez.
///      La separación host/proxy evita el doble disparo.
///
///   3) Take damage (trigger): NO usa tick stamp propio.
///      Lo dispara EnemyHealth.CurrentHealthChanged() — que ya corre en todos
///      los peers vía [OnChangedRender] sobre CurrentHealth. Esto piggybackea
///      sobre un campo networked existente → 0 bandwidth extra. Cada peer
///      dispara localmente exactamente una vez por cambio de vida.
///
///   4) Muerte (estado): NetIsDead con OnChangedRender.
///      Cuando flipea a true, el callback dispara deathTrigger en TODOS los
///      peers (incluido el host). Es un ESTADO persistente, no un trigger,
///      así que un cliente que entra tarde y ve el enemigo "muerto pero
///      todavía no despawneado" también ve la pose vía el primer snapshot.
///
/// ANIMATOR PARAMETERS ESPERADOS:
///   bool    isWalking
///   int     idleType
///   trigger attackTrigger
///   trigger takeDamageTrigger
///   trigger deathTrigger
///
/// TAGS ESPERADOS EN STATES DEL AnimatorController:
///   "Attack"      → estados de ataque (para que el randomizer de idle no los pise)
///   "TakeDamage"  → estados de take damage (idem)
/// </summary>
public class NetEnemyAnimator : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;

    [Header("Walk")]
    [Tooltip("Velocidad mínima del NavMeshAgent para considerar que está caminando.")]
    [SerializeField] private float walkSpeedThreshold = 0.1f;

    [Header("Idle Randomizer")]
    [Tooltip("Cantidad de VARIANTES de idle además del idle base (que es idleType=0).\n" +
             "Melee = 1 (base + 1 variante = 2 idles).\n" +
             "Ranged = 2 (base + 2 variantes = 3 idles).")]
    [SerializeField] private int idleVariantCount = 1;
    [Tooltip("Probabilidad de cambiar a una variante cada vez que expira el timer.")]
    [SerializeField, Range(0f, 1f)] private float chanceToChange = 0.15f;
    [Tooltip("Segundos que dura una variante antes de volver al idle base.")]
    [SerializeField] private float variantDuration = 4f;

    // Networked state
    // Solo el host escribe estos campos. Todos los peers los leen en Render().

    [Networked] private NetworkBool NetIsWalking { get; set; }
    [Networked] private int NetIdleType { get; set; }

    // Tick stamp del ataque. Cuando el host lo cambia, los proxies detectan el
    // delta en Render() y disparan attackTrigger una vez.
    [Networked] private int NetAttackTick { get; set; }

    // Estado de muerte. OnChangedRender hace que cuando flipea, todos los peers
    // (incluido el host) disparen deathTrigger vía el callback. Como es ESTADO
    // (no tick), si un cliente entra tarde y el valor ya es true, el callback
    // fire al recibir el primer snapshot → ve la pose de muerto aunque no
    // haya visto la transición.
    [Networked, OnChangedRender(nameof(OnIsDeadChanged))]
    private NetworkBool NetIsDead { get; set; }

    // Local state (no networked)
    // Cache local del último tick observado por proxies para detectar cambios.
    private int _lastAttackTick;

    // Estado del idle randomizer del host. Usa Random.value (no determinístico),
    // por eso vive solo en el host y no se replica.
    private float _serverIdleTimer;
    private bool _serverIsInVariant;

    // Cached Animator parameter hashes
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private static readonly int IdleTypeHash = Animator.StringToHash("idleType");
    private static readonly int AttackTriggerHash = Animator.StringToHash("attackTrigger");
    private static readonly int TakeDamageTriggerHash = Animator.StringToHash("takeDamageTrigger");
    private static readonly int DeathTriggerHash = Animator.StringToHash("deathTrigger");

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    public override void Spawned()
    {
        // Inicializamos el cache para que un proxy que entra tarde no dispare
        // el trigger de ataque al recibir el primer snapshot (cuyo NetAttackTick
        // probablemente sea != 0).
        _lastAttackTick = NetAttackTick;
    }

    //  HOST-ONLY LOGIC (FixedUpdateNetwork)

    public override void FixedUpdateNetwork()
    {
        // Solo el host conduce al enemigo: no hay InputAuthority en enemigos.
        if (!Object.HasStateAuthority) return;
        // Si murió, congelamos todo lo demás — el deathTrigger ya se está
        // reproduciendo y no queremos pisarlo con isWalking ni con idle.
        if (NetIsDead) return;
        // Evitar duplicaciones durante resimulación, igual que en el player.
        // Los cambios de [Networked] solo deben aplicarse en ticks Forward.
        if (!Runner.IsForward) return;

        UpdateIsWalking();
        UpdateIdleRandomizer();
    }

    private void UpdateIsWalking()
    {
        // agent.velocity refleja el movimiento real del NavMeshAgent. Más confiable
        // que "hasPath" o "remainingDistance" porque incluye los frames donde
        // el path ya está calculado pero el agent todavía no arrancó a moverse.
        bool walking = agent != null
                       && agent.velocity.sqrMagnitude > walkSpeedThreshold * walkSpeedThreshold;
        NetIsWalking = walking;
    }

    private void UpdateIdleRandomizer()
    {
        // Condición pedida: no caminando + no muerto + no atacando + no recibiendo daño.
        // Las dos últimas las leemos del propio Animator vía tags del state actual.
        if (NetIsWalking || NetIsDead || IsInBusyAnimState())
        {
            ResetIdle();
            return;
        }
        // Sin variantes configuradas, no randomizamos (ej: si en el futuro algún
        // enemigo tiene un solo idle).
        if (idleVariantCount <= 0) return;

        _serverIdleTimer -= Runner.DeltaTime;
        if (_serverIdleTimer > 0f) return;

        if (!_serverIsInVariant)
        {
            // Tirar dado para entrar a una variante.
            if (Random.value < chanceToChange)
            {
                // Range(int, int) excluye el upper bound → [1, idleVariantCount] inclusive.
                int choice = Random.Range(1, idleVariantCount + 1);
                NetIdleType = choice;
                _serverIdleTimer = variantDuration;
                _serverIsInVariant = true;
            }
            else
            {
                _serverIdleTimer = 2f; // reintentar más tarde
            }
        }
        else
        {
            // Volver al idle base.
            NetIdleType = 0;
            _serverIdleTimer = 5f;
            _serverIsInVariant = false;
        }
    }

    /// <summary>
    /// ¿El Animator está en un state "ocupado" (ataque o daño)? Si sí, el
    /// randomizer de idle se pausa para no pisarlo. Requiere que los states
    /// correspondientes tengan los tags "Attack" y "TakeDamage" en el
    /// AnimatorController.
    /// </summary>
    private bool IsInBusyAnimState()
    {
        if (animator == null) return false;
        var info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsTag("Attack") || info.IsTag("TakeDamage");
    }

    private void ResetIdle()
    {
        if (NetIdleType != 0) NetIdleType = 0;
        _serverIsInVariant = false;
        _serverIdleTimer = 2f;
    }

    //  PUBLIC API

    /// <summary>
    /// Llamar desde EnemyAI cuando el enemigo emite un ataque (melee o ranged).
    /// Replica el trigger a todos los peers vía tick stamp.
    /// Host-only: la BT solo corre en el host, así que solo el host invoca esto.
    /// </summary>
    public void TriggerAttack()
    {
        if (!Object.HasStateAuthority) return;
        if (NetIsDead) return;

        // Patrón tick stamp: cambiamos el [Networked] → los proxies detectan
        // el delta en Render() y disparan el trigger una vez.
        NetAttackTick = Runner.Tick;

        // Disparo local en el host. El host es owner y NO lee NetAttackTick
        // en Render() (filtra con HasStateAuthority abajo) para evitar el doble disparo.
        animator.SetTrigger(AttackTriggerHash);

        ResetIdle();
    }

    /// <summary>
    /// Llamar desde EnemyHealth.CurrentHealthChanged() cuando detecte una
    /// disminución de vida.
    ///
    /// IMPORTANTE: este método se ejecuta en CADA peer donde se haya disparado
    /// CurrentHealthChanged (que vía [OnChangedRender] fire en todos). No
    /// filtramos por HasStateAuthority a propósito — esa es la razón por la
    /// que take-damage no necesita tick stamp propio: ya viaja "gratis"
    /// piggybackeado sobre el [OnChangedRender] de CurrentHealth.
    /// </summary>
    public void TriggerTakeDamage()
    {
        if (animator == null) return;
        if (NetIsDead) return;

        animator.SetTrigger(TakeDamageTriggerHash);

        // El idle randomizer solo lo modifica el host (es donde corre la lógica).
        if (Object.HasStateAuthority) ResetIdle();
    }

    /// <summary>
    /// Llamar desde EnemyHealth.Die() para entrar en estado muerto.
    /// Solo el host puede invocarlo. El SetTrigger del deathTrigger lo
    /// dispara OnIsDeadChanged() vía [OnChangedRender] en todos los peers
    /// (incluido el host) → un único punto de disparo, consistente para todos.
    /// </summary>
    public void SetDead()
    {
        if (!Object.HasStateAuthority) return;
        if (NetIsDead) return;

        NetIsDead = true;
        // Forzamos walking off y idle base por las dudas que el BT/agent no
        // hayan parado todavía al momento exacto de la muerte — evita un
        // frame de mezcla walk+death en los proxies.
        NetIsWalking = false;
        NetIdleType = 0;
    }

    //  RENDER LOGIC (corre en TODOS los peers a framerate de pantalla)

    public override void Render()
    {
        if (animator == null) return;

        // Bools / ints: idempotentes, los aplicamos siempre en todos los peers.
        animator.SetBool(IsWalkingHash, NetIsWalking);
        animator.SetInteger(IdleTypeHash, NetIdleType);

        // El host ya disparó SetTrigger en TriggerAttack(). Solo los proxies
        // necesitan reaccionar al cambio de NetAttackTick para no duplicar.
        if (Object.HasStateAuthority) return;

        if (NetAttackTick != _lastAttackTick)
        {
            _lastAttackTick = NetAttackTick;
            animator.SetTrigger(AttackTriggerHash);
        }

        // takeDamageTrigger y deathTrigger NO se manejan acá:
        //   - takeDamage lo dispara EnemyHealth.CurrentHealthChanged() en cada peer.
        //   - death lo dispara OnIsDeadChanged() vía [OnChangedRender] en cada peer.
    }

    /// <summary>
    /// Callback de Fusion cuando NetIsDead cambia. Fire en todos los peers
    /// (incluido el host) → dispara el deathTrigger una vez por peer.
    /// </summary>
    private void OnIsDeadChanged()
    {
        if (NetIsDead && animator != null)
        {
            animator.SetTrigger(DeathTriggerHash);
        }
    }
}
