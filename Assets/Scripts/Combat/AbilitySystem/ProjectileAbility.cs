using UnityEngine;
using Fusion;

[CreateAssetMenu(menuName = "Abilities/Projectile Ability")]
public class ProjectileAbility : Ability
{
    public float colliderLifeTime = 3f;
    public float objectLifeTime = 3f;
    public NetworkObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float damageMultiplier = 1f;
    public bool destroyOnHit = true;
    public ProjectileType projectileType;
    public bool showHitVFX = true;

    [Header("AOE")]
    public bool isAoe = false;
    [Header("On Expire AOE")]
    public bool spawnOnExpireAoe = false;
    public NetworkObject onExpirePrefab;
}

public enum ProjectileType
{
    Player,
    Enemy
}