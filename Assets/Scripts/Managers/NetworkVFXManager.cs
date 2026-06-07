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
    public void RPC_SpawnProjectileHitVFX(Vector3 position, bool damagedTarget)
    {
        // Efecto de colision — siempre (pared, suelo, enemigo, etc.)
        if (projectileCollisionVFX != null)
        {
            var vfx = Instantiate(projectileCollisionVFX, position, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        // Efecto de daño al objetivo — solo si daño y tiene prefab asignado
        if (damagedTarget && projectileHitTargetVFX != null)
        {
            var vfx = Instantiate(projectileHitTargetVFX, position, Quaternion.identity);
            Destroy(vfx, 5f);
        }
    }
}