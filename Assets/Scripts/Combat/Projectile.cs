using System.Collections.Generic;
using Fusion;
using Game.CameraSystem;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    // ── Estado networked del proyectil ───────────────────────────────────────
    [Networked] private Vector3 Direction { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private int Damage { get; set; }
    [Networked] private bool hasHit { get; set; }
    [Networked] private NetworkObject Owner { get; set; }
    [Networked] private ProjectileType Type { get; set; }
    [Networked] private bool DestroyOnHit { get; set; }

    /// <summary>
    /// Tiempo de vida visual del GameObject completo (incluye los hijos VFX
    /// del prefab, como trails/swishes). Puede ser MAYOR que el tiempo de vida
    /// del collider, para que el efecto visual del proyectil siga viendose
    /// un rato despues de que deja de detectar colisiones.
    /// Se cuenta desde el momento en que el collider se desactiva (por hit
    /// o por expiracion), no desde el spawn.
    /// </summary>

    /// <summary>
    /// Cuenta regresiva hacia el Despawn real del GameObject, una vez que
    /// el collider ya se desactivo. -1 significa que todavia no arranco.
    /// </summary>


    /// <summary>
    /// Si es true, manda el VFX de explosion al impactar via NetworkVFXManager.
    /// Se desactiva para ataques melee que no necesitan explosion.
    /// </summary>
    [Networked] private bool ShowHitVFX { get; set; }

    // ── Estado networked para VFX de expiracion ──────────────────────────────
    /// <summary>
    /// Se activa cuando el proyectil expira por tiempo (sin golpear nada).
    /// El Despawn se hace un tick despues para que Render() lo detecte primero.
    /// </summary>
    [Networked] private NetworkBool Expired { get; set; }

    /// <summary>
    /// Posicion exacta de la expiracion. Se escribe antes de activar Expired
    /// para que Render() la lea en la posicion correcta.
    /// </summary>
    [Networked] private Vector3 ExpirePosition { get; set; }

    /// <summary>
    /// Tick en que ocurrio el hit. Usado para garantizar que el Despawn
    /// ocurra en el tick SIGUIENTE al hit, no en el mismo.
    /// -1 significa que no hubo hit todavia.
    /// Se inicializa en Initialize() y no en Spawned() para garantizar
    /// que sea -1 desde el primer tick antes de que FixedUpdateNetwork corra.
    /// </summary>
    [Networked] private int HitTick { get; set; }

    [Header("VFX (asignar en Inspector)")]
    [SerializeField] private GameObject collisionVFX;   // efecto al expirar sin chocar
    [SerializeField] private GameObject hitTargetVFX;   // efecto al danar un objetivo (opcional)
    private bool IsAoe;
    private HashSet<NetworkObject> hitTargets = new HashSet<NetworkObject>();

    // ── Guard de VFX de impacto, por-target ──────────────────────────────────
    // No-AOE: el collider se desactiva en el primer hit, asi que esta lista
    // nunca pasa de 1 elemento -> el VFX sale una sola vez.
    // AOE: el collider sigue activo, asi que cada enemigo nuevo agrega su
    // propia entrada -> un VFX por enemigo golpeado, sin duplicarse si el
    // mismo enemigo dispara OnTriggerEnter mas de una vez (colliders compuestos).
    private HashSet<NetworkObject> vfxSentTo = new HashSet<NetworkObject>();

    private Collider col;

    // Variable local — evita que el VFX de expiracion se instancie mas de una vez por peer
    private bool _vfxExpiredPlayed;
    private bool onExpireAoe = false;
    private NetworkObject onExpirePrefab = null;
    private bool _aoeSpawned = false; // ← flag para evitar spawnear el AOE varias veces
    [Networked] private TickTimer ColliderTimer { get; set; }
    [Networked] private TickTimer ObjectTimer { get; set; }
    [Networked] private NetworkBool _vfxExpireSent { get; set; }

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.enabled = false;
    }

    public override void Spawned()
    {
        // Solo el cliente que disparo destruye su proyectil falso local
        
    }

    public void Initialize(NetworkObject caster, int damage, float speed, Vector3 dir,
                           float colliderLifeTime, float objectLifeTime, bool destroyOnHit, ProjectileType type, bool showHitVFX, bool isAoe, bool isOnExpireAoe, NetworkObject onExpirePrefabNW)
    {
        Owner = caster;
        Damage = damage;
        Speed = speed;
        Direction = dir.normalized;
        ColliderTimer = TickTimer.CreateFromSeconds(Runner, colliderLifeTime);
        ObjectTimer = TickTimer.CreateFromSeconds(Runner, objectLifeTime);

        HitTick = -1;
        DestroyOnHit = destroyOnHit;
        Type = type;
        ShowHitVFX = showHitVFX;
        IsAoe = isAoe;
        // Se inicializa acá y no en Spawned() para garantizar que sea -1
        // desde el primer tick, antes de que FixedUpdateNetwork corra.
        HitTick = -1;
        onExpireAoe = isOnExpireAoe;
        onExpirePrefab = onExpirePrefabNW;

        if (col == null) col = GetComponent<Collider>();
        col.enabled = true;
    }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
                return;

            if (!hasHit)
                transform.position += Direction * Speed * Runner.DeltaTime;

            // Un solo bloque — con el RPC adentro
            if (col.enabled && ColliderTimer.Expired(Runner))
            {
                col.enabled = false;
                ExpirePosition = transform.position;
                Expired = true;
                SpawnOnExpireAoe(ExpirePosition);

                if (ShowHitVFX && NetworkVFXManager.Instance != null && !_vfxExpireSent)
                {
                    _vfxExpireSent = true;
                    NetworkVFXManager.Instance.RPC_SpawnProjectileHitVFX(
                        ExpirePosition, false, Type, Direction);
                }
            }

            if (DestroyOnHit && hasHit && HitTick != -1 && Runner.Tick > HitTick)
            {
                Runner.Despawn(Object);
                return;
            }

            if (ObjectTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }
        }

    /// <summary>
    /// Arranca la cuenta regresiva hacia el Despawn real del GameObject,
    /// usando ObjectLifeTime. El collider ya deberia estar desactivado
    /// para este punto (por hit o por expiracion).
    /// </summary>
private bool _wallHitVFXSent = false;

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.HasStateAuthority) return;
        if (hasHit && !IsAoe) return; // solo bloquea si NO es aoe
        if (Owner == null) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) return;

        var otherNet = other.GetComponent<NetworkObject>();
        if (otherNet != null && otherNet == Owner) return;
        if (otherNet != null && hitTargets.Contains(otherNet)) return; // evita doble daño

        bool damagedTarget = false;

        var damageable = other.GetComponent<IDamageable>();
        

        if (damageable == null && ShowHitVFX && collisionVFX != null && !_wallHitVFXSent)
        {
            _wallHitVFXSent = true;
            Vector3 vfxPos = other.ClosestPoint(transform.position);
            var vfx = Instantiate(collisionVFX, vfxPos, Quaternion.identity);
            vfx.transform.SetParent(null);
        }
        
        if (damageable != null)
        {
            if (Type == ProjectileType.Player && !other.CompareTag("Enemy")) goto skip;
            if (Type == ProjectileType.Enemy && !other.CompareTag("Player")) goto skip;

            if (otherNet != null) hitTargets.Add(otherNet);

            damageable.TakeDamage(Damage, Owner.gameObject);
            damagedTarget = true;
            if (Object.HasStateAuthority && Owner != null && Owner.HasInputAuthority)
            {
                CameraController.Local?.Shake(0.08f, 0.2f);
            }
        }

    skip:
        if (!IsAoe)
        {
            hasHit = true;
            col.enabled = false;
            Speed = 0f;      // Solo se frena si DestroyOnHit + no AOE
            HitTick = Runner.Tick;
        }
        if (onExpireAoe) SpawnOnExpireAoe(transform.position); // ← al impactar si es AOE de expiracion

        // ── VFX de impacto ────────────────────────────────────────────────────
        // Usamos "damageable != null" en lugar de "damagedTarget" para que el VFX
        // se dispare aunque el daño haya sido bloqueado (ej: PvP entre jugadores,
        // bloqueado arriba por el chequeo de Type+Tag que salta a este label).
        // damagedTarget se sigue pasando al RPC para que elija el VFX correcto
        // (impacto a objetivo vs colisión genérica) según corresponda.
        // Solo una vez por target — igual que antes.
        if (damageable != null && ShowHitVFX && NetworkVFXManager.Instance != null)
        {
            bool alreadySent = otherNet != null && vfxSentTo.Contains(otherNet);
            if (!alreadySent)
            {
                if (otherNet != null) vfxSentTo.Add(otherNet);

                Vector3 vfxPos = other.ClosestPoint(transform.position);
                // Mandamos "true" para que el VFX use la rama de impacto a objetivo (sin
                // offset), ya que golpeó algo dañable (un jugador) aunque no se aplicara
                // daño real por el bloqueo de PvP. damagedTarget ya no sirve para esto.
                NetworkVFXManager.Instance.RPC_SpawnProjectileHitVFX(vfxPos, true, Type, Direction);
            }
        }
    }

    private void SpawnOnExpireAoe(Vector3 position)
    {
        if (!onExpireAoe || onExpirePrefab == null) return;
        if (_aoeSpawned) return; // ← guard inmediato
        _aoeSpawned = true;

        var cachedOwner = Owner;
        var cachedDamage = Damage;
        var cachedType = Type;

        Runner.Spawn(
            onExpirePrefab,
            position,
            Quaternion.identity,
            inputAuthority: null,
            (r, obj) =>
            {
                obj.GetComponent<Projectile>()?.Initialize(
                    cachedOwner,
                    cachedDamage,
                    0f,
                    Vector3.zero,
                    .3f,
                    .3f,
                    false,
                    cachedType,
                    false,
                    true,
                    false,
                    null
                );
            }
        );
    }

    /// <summary>
    /// Render() corre en TODOS los peers a framerate de pantalla.
    /// Maneja el VFX de expiracion por tiempo — los hits los maneja
    /// el NetworkVFXManager via RPC, o LocalMeleeHitVFX.cs para el caso melee.
    /// El collider y el Despawn real ahora tienen tiempos independientes
    /// (ver ActiveTime/ObjectLifeTime en FixedUpdateNetwork), asi que el VFX
    /// de expiracion sigue siendo visible mientras el objeto vive su
    /// ObjectLifeTime extra.
    /// </summary>
    public override void Render()
    {

    }
}