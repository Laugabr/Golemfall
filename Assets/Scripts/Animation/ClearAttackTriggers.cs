using UnityEngine;

public class ClearAttackTriggers : StateMachineBehaviour
{
    private static readonly int MeleeTrigger = Animator.StringToHash("meleeTrigger");
    private static readonly int RangeTrigger = Animator.StringToHash("rangeTrigger");

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger(MeleeTrigger);
        animator.ResetTrigger(RangeTrigger);
    }
}
