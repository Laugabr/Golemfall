using UnityEngine;
using Fusion;

[CreateAssetMenu(menuName = "Abilities/Projectile Ability")]
public class ProjectileAbility : Ability
{
    public NetworkObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float damageMultiplier = 1f;
    public bool destroyOnHit = true;
}