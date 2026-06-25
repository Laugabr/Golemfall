using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Muestra el VFX de impacto de un ataque melee de forma LOCAL en cada peer,
/// sin pasar por estado networked ni por el RPC de NetworkVFXManager.
///
/// A diferencia de Projectile.OnTriggerEnter (que solo corre en el host,
/// vía el chequeo de Object.HasStateAuthority), este componente corre en
/// TODOS los clientes a la vez, porque no tiene ese chequeo. Cada peer ve
/// la posicion interpolada/replicada del proyectil y detecta el trigger
/// de forma independiente.
///
/// Tradeoff aceptado: como cada cliente detecta el trigger localmente
/// (no es el servidor informando el resultado), puede haber una pequeña
/// discrepancia de timing/posicion entre peers con lag. El daño real
/// sigue siendo decidido unicamente por Projectile.cs en el host —
/// este componente NUNCA debe llamar a TakeDamage ni nada que afecte
/// el estado de juego, solo instancia VFX puramente visual.
///
/// Requiere que el collider del proyectil sea is-trigger (igual config
/// que usa Projectile.cs). Puede compartir el mismo Collider del
/// GameObject; Unity llama OnTriggerEnter en todos los componentes del
/// mismo objeto, no solo en uno.
/// </summary>
public class LocalMeleeHitVFX : MonoBehaviour
{
    [Header("VFX (asignar en Inspector)")]
    [SerializeField] private GameObject hitVfx;

    [Header("Filtros (deben coincidir con la config del ataque)")]
    [SerializeField] private ProjectileType type = ProjectileType.Player;
    [SerializeField] private bool isAoe = true;

    // Evita instanciar VFX mas de una vez para el mismo objetivo en este peer.
    // Usamos el GameObject en vez de NetworkObject porque esto corre tambien
    // en clientes sin autoridad, y no necesitamos red para este chequeo.
    private readonly HashSet<GameObject> _vfxShownTo = new HashSet<GameObject>();

    // Si no es AOE, paramos despues del primer impacto (igual que Projectile).
    private bool _hasHitOnce;

    /// <summary>
    /// Llamado desde el lugar donde se configura el proyectil (junto con
    /// Projectile.Initialize), para que los filtros coincidan con el ataque real.
    /// </summary>
    public void Configure(ProjectileType projectileType, bool aoe)
    {
        type = projectileType;
        isAoe = aoe;
        _vfxShownTo.Clear();
        _hasHitOnce = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hitVfx == null) return;
        if (!isAoe && _hasHitOnce) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) return;
        if (_vfxShownTo.Contains(other.gameObject)) return;

        // Mismos filtros de tipo que usa Projectile.OnTriggerEnter, para que
        // el VFX local solo aparezca donde el daño real tambien aplicaria.
        if (type == ProjectileType.Player && !other.CompareTag("Enemy")) return;
        if (type == ProjectileType.Enemy && !other.CompareTag("Player")) return;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return; // solo VFX si el target es daniable

        _vfxShownTo.Add(other.gameObject);
        if (!isAoe) _hasHitOnce = true;

        var vfx = Instantiate(hitVfx, other.bounds.center, Quaternion.identity);
        Destroy(vfx, 5f);
    }
}