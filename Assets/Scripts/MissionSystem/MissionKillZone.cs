using UnityEngine;
using Fusion;

/// <summary>
/// Zona de objetivo basada en ubicación. Cualquier EnemyHealth que muera mientras
/// está dentro de este trigger emite su KillEnemy con esta key, sin necesidad de
/// configurar cada enemigo a mano.
///
/// El matching wildcard de MissionStep hace que un kill con key cuente además como
/// kill genérico, así que NO se dispara un segundo evento (sin doble conteo).
///
/// Solo opera del lado del StateAuthority (host), que es donde EnemyHealth.Die()
/// lee el key y emite el evento de tracking.
///
/// Requisitos en escena:
///  - Este GameObject necesita un Collider con isTrigger = true (Reset lo fuerza).
///  - Los enemigos deben generar eventos de trigger (Collider + Rigidbody, o un
///    CharacterController). Si en tu setup los triggers no disparan contra los
///    enemigos, avisá: la alternativa es chequear la zona por overlap en Die().
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionKillZone : MonoBehaviour
{
    [Tooltip("Key del objetivo. Debe coincidir con el targetKey del MissionStep. " +
             "Para objetivos que toca código, usá una const de ObjectiveKeys.")]
    [SerializeField] private string objectiveKey = "";

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) => Tag(other);

    // Necesario porque muchos enemigos ya nacen DENTRO de la zona:
    // OnTriggerEnter no dispara para ellos, pero OnTriggerStay sí.
    private void OnTriggerStay(Collider other) => Tag(other);

    private void Tag(Collider other)
    {
        var enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null || enemy.Object == null) return;

        // Solo el host arbitra el tracking de misiones.
        if (!enemy.Object.HasStateAuthority) return;

        enemy.SetMissionKey(objectiveKey);
    }

    private void OnTriggerExit(Collider other)
    {
        var enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null || enemy.Object == null) return;

        if (!enemy.Object.HasStateAuthority) return;

        // Limpia solo si este zone es el dueño actual del key (no pisa otra zona).
        enemy.ClearMissionKey(objectiveKey);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
        var col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sph)
        {
            Gizmos.DrawSphere(transform.TransformPoint(sph.center), sph.radius * transform.lossyScale.x);
        }
    }
}