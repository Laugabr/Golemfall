using UnityEngine;

/// <summary>
/// StateMachineBehaviour que dispara el efecto de curación en el frame 11
/// de la animación HealAbility. Se adjunta al estado "HealAbility" en el
/// Animator del jugador.
///
/// Solo ejecuta en el StateAuthority (servidor) — la curación real necesita
/// autoridad para spawnar el UtilityAbility en la red. El cliente ve la
/// animación pero no ejecuta el efecto.
/// </summary>
public class HealAtFrame : StateMachineBehaviour
{
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

        // Calculamos el frame actual en base al tiempo normalizado y el fps del clip
        float clipLength = stateInfo.length;
        var clipInfos = animator.GetCurrentAnimatorClipInfo(layerIndex);
        float fps = clipInfos.Length > 0 ? clipInfos[0].clip.frameRate : 30f;

        int currentFrame = Mathf.FloorToInt(stateInfo.normalizedTime * clipLength * fps);

        if (currentFrame >= targetFrame)
        {
            _fired = true;
            // Solo el StateAuthority ejecuta el efecto — el spawn del UtilityAbility
            // requiere autoridad de red. El cliente solo ve la animación.
            _abilityHolder.ExecuteHealEffect();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Reset por si el estado se interrumpe antes del frame 11
        _fired = false;
    }
}