using UnityEngine;

/// <summary>
/// StateMachineBehaviour que dispara el efecto de curación en un frame
/// configurable desde el Inspector. Se adjunta al estado "HealAbility".
///
/// Solo ejecuta en el StateAuthority (servidor) — la curación real necesita
/// autoridad para spawnar el UtilityAbility en la red.
/// El VFX se manda via NetworkVFXManager para que llegue a todos los peers
/// aunque el objeto se despawnee antes de que el RPC se procese.
/// </summary>
public class HealAtFrame : StateMachineBehaviour
{
    [Tooltip("Frame en el que se ejecuta la curación y el VFX. " +
             "Cambiá este valor si modificás la animación.")]
    [SerializeField] private int targetFrame = 11;

    private bool _fired;
    private AbilityHolder _abilityHolder;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _fired = false;

        if (_abilityHolder == null)
            _abilityHolder = animator.GetComponentInParent<AbilityHolder>();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_fired) return;
        if (_abilityHolder == null) return;

        // Solo el servidor ejecuta el efecto — el cliente solo ve la animación
        if (!_abilityHolder.Object.HasStateAuthority) return;

        float clipLength = stateInfo.length;
        var clipInfos = animator.GetCurrentAnimatorClipInfo(layerIndex);
        float fps = clipInfos.Length > 0 ? clipInfos[0].clip.frameRate : 30f;
        int currentFrame = Mathf.FloorToInt(stateInfo.normalizedTime * clipLength * fps);

        if (currentFrame >= targetFrame)
        {
            _fired = true;

            // Ejecuta la curación en el servidor
            _abilityHolder.ExecuteHealEffect();

            // Manda el VFX a todos los peers via NetworkVFXManager
            if (NetworkVFXManager.Instance != null)
                NetworkVFXManager.Instance.RPC_SpawnHealVFX(
                    animator.transform.position
                );
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _fired = false;
    }
}