using Fusion;
using UnityEngine;

public class ClearAttackLock : StateMachineBehaviour
{
    private NetCharacterController _controller;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_controller == null)
            _controller = animator.GetComponentInParent<NetCharacterController>();
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_controller != null)
            _controller.ClearAttackLock();
    }
}
