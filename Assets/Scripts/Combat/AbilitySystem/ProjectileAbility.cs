using UnityEngine;


[CreateAssetMenu(menuName = "Ability/Projectile Ability")]
public class ProjectileAbility : Ability
{
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float damageMultiplier = 1f;
    [SerializeField] LayerMask groundLayer;
    public override void Activate(GameObject caster)
    {
        // En lugar de instanciar directamente, devolvemos los datos necesarios
        ProjectileRuntime.Execute(this, caster, groundLayer);
    }
}