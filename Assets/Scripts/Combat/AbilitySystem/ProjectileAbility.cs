using UnityEngine;
using Fusion;

[CreateAssetMenu(menuName = "Abilities/Projectile Ability")]
public class ProjectileAbility : Ability
{
    public float projectileLifetime = 3f;
    public NetworkObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float damageMultiplier = 1f;
    public bool destroyOnHit = true;
    public ProjectileType projectileType;
    public bool showHitVFX = true;

    [Header("AOE")]
    public bool isAoe = false;
}

public enum ProjectileType
{
    Player,
    Enemy
}