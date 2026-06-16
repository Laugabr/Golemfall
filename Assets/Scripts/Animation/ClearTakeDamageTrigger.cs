using UnityEngine;

public class ClearTakeDamageTrigger : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger("takeDamageTrigger");
    }
}