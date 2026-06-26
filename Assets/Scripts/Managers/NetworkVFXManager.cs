using Fusion;
using UnityEngine;

/// <summary>
/// Manager de VFX en red. Existe como objeto permanente en la escena
/// y es el responsable de mandar RPCs de efectos visuales.
///
/// Se usa para VFX de proyectiles, destructibles y curación porque el Despawn
/// del objeto puede llegar al cliente antes que un RPC mandado desde el propio
/// objeto. Al mandar el RPC desde este objeto permanente, el cliente siempre
/// lo recibe correctamente.
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

    [Header("VFX de curación")]
    [Tooltip("VFX que se muestra en el frame 11 de la animación de curación.")]
    [SerializeField] private GameObject healVFX;

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

        if (damagedTarget)
        {
            GameObject vfxToSpawn = type == ProjectileType.Player ?
                projectileHitTargetVFX : enemyProjectileCollisionVFX;

            if (vfxToSpawn != null)
            {
                Vector3 finalPos = position + hitDirection.normalized * 2.5f + Vector3.up * 0.2f;
                var vfx = Instantiate(vfxToSpawn, finalPos, rot);
                Destroy(vfx, 5f);
            }
        }
        else
        {
            GameObject vfxToSpawn = type == ProjectileType.Player ?
                projectileCollisionVFX : enemyProjectileCollisionVFX;

            if (vfxToSpawn != null)
            {
                Vector3 finalPos = position + hitDirection.normalized * 5f;
                var vfx = Instantiate(vfxToSpawn, finalPos, rot);
                Destroy(vfx, 5f);
            }
        }
    }

    /// <summary>
    /// Llamado por DestructibleObject cuando un objeto se rompe.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SpawnDestructibleBreakVFX(Vector3 position)
    {
        if (destructibleBreakVFX == null) return;
        var vfx = Instantiate(destructibleBreakVFX, position, Quaternion.identity);
        Destroy(vfx, 5f);
    }

    /// <summary>
    /// Llamado por HealAtFrame cuando la animación de curación llega al frame
    /// configurado. Se manda desde este objeto permanente para garantizar que
    /// llegue a todos los peers aunque el jugador se mueva o haya lag.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SpawnHealVFX(Vector3 position)
    {
        if (healVFX == null) return;
        var vfx = Instantiate(healVFX, position, Quaternion.identity);
        Destroy(vfx, 5f);
    }

    /// <summary>
    /// Desactiva la puerta con el ID dado en todos los peers.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DisableDoor(int doorId)
    {
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