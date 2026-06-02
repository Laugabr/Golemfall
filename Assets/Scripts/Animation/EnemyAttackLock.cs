using UnityEngine;

/// <summary>
/// StateMachineBehaviour que bloquea el movimiento del enemigo
/// mientras se reproduce la animación de ataque.
///
/// Se agrega como behaviour en el estado "Attack" del AnimatorController
/// del enemigo (igual que ClearJumpTrigger en el jugador).
///
/// Al entrar al estado Attack:
///   - Activa el flag IsInAttackAnimation en EnemyAI
///   - Resetea el path y velocidad del NavMeshAgent para detener al enemigo
///
/// Al salir del estado Attack:
///   - Desactiva el flag para que el enemigo pueda moverse de nuevo
/// </summary>
public class EnemyAttackLock : StateMachineBehaviour
{
    // Cacheamos la referencia al EnemyAI para no buscarlo cada frame
    private EnemyAI _ai;

    /// <summary>
    /// Se llama una vez cuando el Animator entra al estado Attack.
    /// </summary>
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Buscamos el EnemyAI en el padre la primera vez que entramos
        if (_ai == null)
            _ai = animator.GetComponentInParent<EnemyAI>();

        if (_ai != null)
        {
            // Activamos el flag para que EnemyAI sepa que está atacando
            _ai.IsInAttackAnimation = true;

            // Detenemos el NavMeshAgent inmediatamente
            _ai.Agent.ResetPath();
            _ai.Agent.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Se llama una vez cuando el Animator sale del estado Attack.
    /// Libera el flag para que el enemigo pueda moverse de nuevo.
    /// </summary>
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_ai != null)
            _ai.IsInAttackAnimation = false;
    }
}