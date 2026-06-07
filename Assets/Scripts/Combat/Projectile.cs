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
    // Para la expiracion usamos flag networked + Render() con Despawn diferido
    // porque el objeto se destruye inmediatamente y cualquier RPC se perderia.
    // Para los hits usamos NetworkVFXManager que siempre existe en la escena,
    // garantizando que el RPC llegue al cliente sin importar el Despawn.

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

    [Header("VFX (asignar en Inspector)")]
    [SerializeField] private GameObject collisionVFX;   // efecto al expirar sin chocar
    [SerializeField] private GameObject hitTargetVFX;   // efecto al danar un objetivo (opcional)

    private HashSet<NetworkObject> hitTargets = new HashSet<NetworkObject>();
    private Collider col;

    // Variable local — evita que el VFX de expiracion se instancie mas de una vez por peer
    private bool _vfxExpiredPlayed;

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
                           float activeTime, bool destroyOnHit, ProjectileType type, bool showHitVFX)
    {
        Owner = caster;
        Damage = damage;
        Speed = speed;
        Direction = dir.normalized;
        ActiveTime = activeTime;
        DestroyOnHit = destroyOnHit;
        Type = type;
        ShowHitVFX = showHitVFX;

        if (col == null) col = GetComponent<Collider>();
        col.enabled = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        ActiveTime -= Runner.DeltaTime;
        transform.position += Direction * Speed * Runner.DeltaTime;

        // Tick 1 — activamos el flag de expiracion y guardamos la posicion.
        // Render() lo detecta en todos los peers en este tick.
        if (ActiveTime <= 0f && !Expired && !hasHit)
        {
            ExpirePosition = transform.position;
            Expired = true;
        }

        // Tick 2 — despawneamos un tick despues para darle tiempo a Render()
        // de correr en todos los peers con el flag ya activado.
        if (Expired && ActiveTime <= -Runner.DeltaTime)
        {
            Runner.Despawn(Object);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.HasStateAuthority) return;
        if (hasHit) return;
        if (Owner == null) return;

        var otherNet = other.GetComponent<NetworkObject>();
        if (otherNet != null && otherNet == Owner) return;

        bool damagedTarget = false;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            if (Type == ProjectileType.Player && !other.CompareTag("Enemy")) goto skip;
            if (Type == ProjectileType.Enemy && !other.CompareTag("Player")) goto skip;

            if (otherNet != null)
            {
                if (hitTargets.Contains(otherNet)) return;
                hitTargets.Add(otherNet);
            }

            damageable.TakeDamage(Damage, Owner.gameObject);
            damagedTarget = true;
        }

    skip:
        hasHit = true;

        // Mandamos el VFX desde el NetworkVFXManager que siempre existe,
        // garantizando que el RPC llegue al cliente antes o despues del Despawn.
        // Solo si ShowHitVFX es true — el melee lo tiene desactivado.
        if (ShowHitVFX && NetworkVFXManager.Instance != null)
            NetworkVFXManager.Instance.RPC_SpawnProjectileHitVFX(transform.position, damagedTarget);

        if (DestroyOnHit)
        {
            col.enabled = false;
            Runner.Despawn(Object);
        }
    }

    /// <summary>
    /// Render() corre en TODOS los peers a framerate de pantalla.
    /// Solo maneja el VFX de expiracion por tiempo — los hits los maneja
    /// el NetworkVFXManager via RPC.
    /// _vfxExpiredPlayed es local por peer para evitar instanciar el VFX
    /// multiples veces en frames consecutivos.
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