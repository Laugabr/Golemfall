using UnityEngine;

/// <summary>
/// Dispara el proyectil en el momento exacto de la animación de ataque.
/// Solo el host ejecuta el disparo ya que es quien tiene StateAuthority sobre el enemigo.
/// Los clientes ven la animación pero no disparan proyectiles.
/// </summary>
public class EnemyFireAtFrame : StateMachineBehaviour
{
    [Tooltip("Momento normalizado (0 a 1) donde sale el proyectil.")]
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
        if (_ai == null) return;

        Debug.Log($"[FIRE] HasStateAuthority: {_ai.Object?.HasStateAuthority}, normalizedTime: {stateInfo.normalizedTime:F2}");

        if (!_ai.Object.HasStateAuthority) return;

        if (stateInfo.normalizedTime >= fireAtNormalizedTime)
        {
            _hasFired = true;
            _ai.FireProjectile();
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _hasFired = false;
    }
}