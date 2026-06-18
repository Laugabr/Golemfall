using UnityEngine;

public class StopWalkVFX : StateMachineBehaviour
{
    [SerializeField] private string vfxChildName = "WalkDustVFX";
    private ParticleSystem _vfx;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo info, int layerIndex)
    {
        if (_vfx == null)
            _vfx = animator.transform.Find(vfxChildName)?.GetComponent<ParticleSystem>();
        _vfx?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}