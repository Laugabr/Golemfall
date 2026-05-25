using UnityEngine;

public class ClearJumpTrigger : StateMachineBehaviour
{
    private static readonly int JumpTrigger = Animator.StringToHash("jumpTrigger");

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger(JumpTrigger);
    }
}