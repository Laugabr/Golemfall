using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    // ── Estado networked del proyectil ───────────────────────────────────────
    [Networked] private Vector3 Direction { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private int Damage { get; set; }
    [Networked] private float ActiveTime { get; set; }
    [Networked] private bool hasHit { get; set; }
    [Networked] private NetworkObject Owner { get; set; }
    [Networked] private ProjectileType Type { get; set; }
    [Networked] private bool DestroyOnHit { get; set; }

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
    private Collider col;

    // Variable local — evita que el VFX de expiracion se instancie mas de una vez por peer
    private bool _vfxExpiredPlayed;
    private bool onExpireAoe = false;
    private NetworkObject onExpirePrefab = null;
    private bool _aoeSpawned = false; // ← flag para evitar spawnear el AOE varias veces
    private void Awake()
    {
        col = GetComponent<Collider>();
        col.enabled = false;
    }

    public override void Spawned()
    {
        // Solo el cliente que disparo destruye su proyectil falso local
        if (Object.HasStateAuthority || !Object.HasInputAuthority) return;

        uint ownerId = Owner.Id.Raw;
        var fake = FakeProjectileRegistry.Dequeue(ownerId);

        if (fake != null) Destroy(fake.gameObject);
    }

    public void Initialize(NetworkObject caster, int damage, float speed, Vector3 dir,
                           float activeTime, bool destroyOnHit, ProjectileType type, bool showHitVFX, bool isAoe, bool isOnExpireAoe, NetworkObject onExpirePrefabNW)
    {
        Owner = caster;
        Damage = damage;
        Speed = speed;
        Direction = dir.normalized;
        ActiveTime = activeTime;
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
        if (!Object.HasStateAuthority) return;

        ActiveTime -= Runner.DeltaTime;
        transform.position += Direction * Speed * Runner.DeltaTime;

        // ── Expiracion por tiempo ────────────────────────────────────────────
        // Tick 1 — activamos el flag y guardamos la posicion
        // Cubre tanto proyectiles normales como melee (DestroyOnHit=false)
        if (ActiveTime <= 0f && !Expired)
        {
            ExpirePosition = transform.position;
            Expired = true;
            SpawnOnExpireAoe(ExpirePosition);
        }

        // Tick 2 — Despawn un tick despues para que Render() lo detecte primero
        if (Expired && ActiveTime <= -Runner.DeltaTime)
        {
            Runner.Despawn(Object);
            return;
        }

        // ── Hit diferido ─────────────────────────────────────────────────────
        // OnTriggerEnter guarda el tick en que ocurrio el hit (HitTick).
        // Esperamos a que el tick actual sea MAYOR al tick del hit para
        // garantizar que el Despawn ocurre en el tick siguiente, no en el mismo.
        if (DestroyOnHit && HitTick >= 0 && Runner.Tick > HitTick)
        {
            Runner.Despawn(Object);
            return;
        }
    }

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
    if (damageable != null)
    {
        if (Type == ProjectileType.Player && !other.CompareTag("Enemy")) goto skip;
        if (Type == ProjectileType.Enemy && !other.CompareTag("Player")) goto skip;

        if (otherNet != null) hitTargets.Add(otherNet);

        damageable.TakeDamage(Damage, Owner.gameObject);
        damagedTarget = true;
    }

skip:
    if (!IsAoe)
    {
        // Comportamiento normal — para en el primer impacto
        hasHit = true;
        col.enabled = false;
        HitTick = Runner.Tick;
    }
    if(onExpireAoe) SpawnOnExpireAoe(transform.position); // ← al impactar si es AOE de expiracion

    if (ShowHitVFX && NetworkVFXManager.Instance != null)
    {
        Vector3 vfxPos = damagedTarget ?
            other.bounds.center :
            other.ClosestPoint(transform.position);
        NetworkVFXManager.Instance.RPC_SpawnProjectileHitVFX(vfxPos, damagedTarget, Type, Direction);
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
        var cachedShowVFX = ShowHitVFX;

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
                    .5f,
                    false,
                    cachedType,
                    cachedShowVFX,
                    true,
                    false,
                    null
                );
            }
        );
    }

    /// <summary>
    /// Render() corre en TODOS los peers a framerate de pantalla.
    /// Solo maneja el VFX de expiracion por tiempo — los hits los maneja
    /// el NetworkVFXManager via RPC.
    /// </summary>
    public override void Render()
    {
        if (Expired && !_vfxExpiredPlayed)
        {
            _vfxExpiredPlayed = true;

            if (collisionVFX != null)
            {
                var vfx = Instantiate(collisionVFX, ExpirePosition, Quaternion.identity);
                Destroy(vfx, 5f);
            }
        }
    }
}