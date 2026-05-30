using UnityEngine;
using Fusion;

[CreateAssetMenu(menuName = "Abilities/Projectile Ability")]
public class ProjectileAbility : Ability
{
    public float projectileLifetime = 3f; // vida del proyectil, independiente del cooldown
    public NetworkObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float damageMultiplier = 1f;
    public bool destroyOnHit = true;
    public ProjectileType projectileType;

}

public enum ProjectileType
{
    Player,
    Enemy
}