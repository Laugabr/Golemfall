using UnityEngine;

/// <summary>
/// Dispara el proyectil en el momento exacto de la animación de ataque
/// usando el normalizedTime del Animator en lugar de Animation Events.
/// Más fácil de ajustar que un Animation Event en el FBX.
/// </summary>
public class EnemyFireAtFrame : StateMachineBehaviour
{
    [Tooltip("Momento normalizado (0 a 1) donde sale el proyectil. " +
             "0 = inicio, 0.5 = mitad, 1 = final")]
    [SerializeField, Range(0f, 1f)] private float fireAtNormalizedTime = 0.5f;

    private bool _hasFired;
    private EnemyAI _ai;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _hasFired = false;
        if (_ai == null)
            _ai = animator.GetComponentInParent<EnemyAI>();
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_hasFired) return;

        // Dispara cuando la animación llega al porcentaje configurado
        if (stateInfo.normalizedTime >= fireAtNormalizedTime)
        {
            _hasFired = true;
            _ai?.FireProjectile();
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _hasFired = false;
    }
}