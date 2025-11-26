using UnityEngine;

public static class ProjectileRuntime
{
    public static void Execute(ProjectileAbility data, GameObject caster, LayerMask groundMask)
    {
        // 1) RAY DEL MOUSE
        Plane plane = new Plane(Vector3.up, caster.transform.position);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Vector3 targetPoint;

        if (plane.Raycast(ray, out float distance))
        {
            targetPoint = ray.GetPoint(distance);
        }
        else
        {
            // fallback raro pero seguro
            targetPoint = caster.transform.position + caster.transform.forward * 5f;
        }

        // 4) POSICIÓN DE DISPARO (levantar un poco del piso)
        Vector3 positionShooting = caster.transform.position + Vector3.up * 1f;

        // 5) DIRECCIÓN
        Vector3 direction = targetPoint - positionShooting;

        // si querés top-down puro → ignorar altura
        direction.y = 0f;
        direction.Normalize();

        // 6) ROTACIÓN CORRECTA DEL PROYECTIL
        Quaternion rotation = Quaternion.LookRotation(direction);

        // 7) INSTANCIAR PROYECTIL
        GameObject obj = Object.Instantiate(
            data.projectilePrefab,
            positionShooting,
            rotation
        );

        // 8) OBTENER STATS
        CharacterStats stats = caster.GetComponent<CharacterStats>();
        if (stats == null)
        {
            Debug.LogError("No CharacterStats on " + caster.name);
            return;
        }

        // 9) INICIALIZAR EL PROYECTIL
        Projectile projectile = obj.GetComponent<Projectile>();
        if (projectile != null)
        {
            float finalDamage = stats.GetStat(Stat.damage) * data.damageMultiplier;

            projectile.Initialize(
                caster,
                Mathf.FloorToInt(finalDamage),
                data.projectileSpeed,
                direction
            );

            Debug.Log("Caster is " + caster.name);


        }
        else
        {
            Debug.LogError("Projectile component missing on " + obj.name);
        }
    }
}