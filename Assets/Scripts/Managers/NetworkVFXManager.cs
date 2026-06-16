using Fusion;
using UnityEngine;

/// <summary>
/// Manager de VFX en red. Existe como objeto permanente en la escena
/// y es el responsable de mandar RPCs de efectos visuales.
///
/// Se usa para VFX de proyectiles y destructibles porque el Despawn del objeto
/// puede llegar al cliente antes que un RPC mandado desde el propio objeto,
/// haciendo que el efecto nunca se vea. Al mandar el RPC desde este objeto
/// que nunca se destruye, el cliente siempre lo recibe correctamente.
/// </summary>
public class NetworkVFXManager : NetworkBehaviour
{
    public static NetworkVFXManager Instance { get; private set; }

    [Header("VFX de proyectil")]
    [SerializeField] private GameObject projectileCollisionVFX;
    [SerializeField] private GameObject projectileHitTargetVFX;
    [SerializeField] private GameObject enemyProjectileCollisionVFX;

    [Header("VFX de destructibles")]
    [Tooltip("VFX que se muestra cuando se rompe una flor u objeto destructible.")]
    [SerializeField] private GameObject destructibleBreakVFX;

    public override void Spawned()
    {
        Instance = this;
    }

    /// <summary>
    /// Llamado por Projectile.cs desde el servidor cuando el proyectil golpea algo.
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

    /// <summary>
    /// Llamado por DestructibleObject desde el servidor cuando un objeto se rompe.
    /// Al mandarse desde este objeto permanente, el RPC siempre llega al cliente
    /// aunque el objeto destructible ya haya sido despawneado.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SpawnDestructibleBreakVFX(Vector3 position)
    {
        if (destructibleBreakVFX == null) return;

        var vfx = Instantiate(destructibleBreakVFX, position, Quaternion.identity);
        Destroy(vfx, 5f);
    }

    /// <summary>
    /// Desactiva la puerta con el ID dado en todos los peers.
    /// Llamado por OnDestroyUnlockCollider antes del despawn del DestructibleObject,
    /// garantizando que el cliente reciba el RPC aunque el objeto ya se haya despawneado.
    /// Desactiva el objeto entero incluyendo la niebla y el collider.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DisableDoor(int doorId)
    {
        // Buscamos todas las puertas en la escena y desactivamos la que corresponde.
        var doors = FindObjectsByType<NetworkDoor>(FindObjectsSortMode.None);
        foreach (var door in doors)
        {
            if (door.DoorId == doorId)
            {
                door.Disable();
                break;
            }
        }
    }
}