using UnityEngine;

public class WalkStateBehaviour : StateMachineBehaviour
{
    [SerializeField] private string vfxChildName = "WalkDustVFX";
    private ParticleSystem _vfx;

    private void FindVFX(Animator animator)
    {
        // Busca en TODA la jerarquía, no solo hijos directos
        var transforms = animator.transform.root.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (t.name == vfxChildName)
            {
                _vfx = t.GetComponent<ParticleSystem>();
                break;
            }
        }

        if (_vfx == null)
            Debug.LogWarning($"[WalkStateBehaviour] No se encontró '{vfxChildName}' en la jerarquía.");
    }

    override public void OnStateEnter(Animator animator, AnimatorStateInfo info, int layerIndex)
    {
        if (_vfx == null) FindVFX(animator);
        _vfx?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _vfx?.Play(true);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo info, int layerIndex)
    {
        _vfx?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}