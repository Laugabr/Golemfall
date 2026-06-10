using Fusion;
using UnityEngine;

/// <summary>
/// Manager de VFX en red. Existe como objeto permanente en la escena
/// y es el responsable de mandar RPCs de efectos visuales.
///
/// Se usa para VFX de proyectiles porque el Despawn del proyectil puede
/// llegar al cliente antes que un RPC mandado desde el propio proyectil,
/// haciendo que el efecto nunca se vea. Al mandar el RPC desde este objeto
/// que nunca se destruye, el cliente siempre lo recibe correctamente.
/// </summary>
public class NetworkVFXManager : NetworkBehaviour
{
    public static NetworkVFXManager Instance { get; private set; }

    [Header("VFX de proyectil (asignar en Inspector)")]
    [SerializeField] private GameObject projectileCollisionVFX;  // explosion al chocar con cualquier cosa
    [SerializeField] private GameObject projectileHitTargetVFX;  // explosion adicional al dañar objetivo (opcional)
    [SerializeField] private GameObject enemyProjectileCollisionVFX; // vfx_RangedEnemy_01

    public override void Spawned()
    {
        Instance = this;
    }

    /// <summary>
    /// Llamado por Projectile.cs desde el servidor cuando el proyectil golpea algo.
    /// Al mandarse desde este objeto que nunca se destruye, el RPC siempre
    /// llega al cliente correctamente — a diferencia de mandarlo desde el
    /// proyectil que puede despawnearse antes de que el RPC se procese.
    /// </summary>

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SpawnProjectileHitVFX(Vector3 position, bool damagedTarget, ProjectileType type, Vector3 hitDirection)
    {
        Quaternion rot = hitDirection != Vector3.zero ?
            Quaternion.LookRotation(-hitDirection) : Quaternion.identity;

        GameObject vfxToSpawn = type == ProjectileType.Player ?
            projectileCollisionVFX : enemyProjectileCollisionVFX;

        if (vfxToSpawn != null)
        {
            Vector3 finalPos = position + hitDirection.normalized * 2.5f;
            var vfx = Instantiate(vfxToSpawn, finalPos, rot);
            Destroy(vfx, 5f);
        }

        if (type == ProjectileType.Player && damagedTarget && projectileHitTargetVFX != null)
        {
            var vfx = Instantiate(projectileHitTargetVFX, position, rot);
            Destroy(vfx, 5f);
        }
    }
}