using System.Collections;
using UnityEngine;

public class HitAnimator : MonoBehaviour, IDamageable
{
    private Animator _animator;
    private WaitForSeconds _hitDuration = new WaitForSeconds(0.1f);
    private static readonly int WasHit = Animator.StringToHash("wasHit");

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(int amount, GameObject source)
    {
        Debug.Log($"[HitAnimator] {gameObject.name} was hit by {source.name} for {amount} damage.");
        _animator.SetBool(WasHit, true);
        StartCoroutine(ResetHit());
    }

    IEnumerator ResetHit()
    {
        yield return _hitDuration;
        _animator.SetBool(WasHit, false);
    }

}