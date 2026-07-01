using UnityEngine;

/// <summary>
/// Traduce Animation Events en SFX (SRP: solo eso). Va en el MISMO GameObject que
/// el Animator, porque Unity busca los métodos de Animation Event en los
/// componentes de ese objeto.
///
/// Por qué Animation Events y no código: las animaciones ya están replicadas por
/// NetCharacterAnimator / NetEnemyAnimator (todos los peers reproducen el mismo
/// clip). Si el sonido sale desde un evento DENTRO del clip, suena en todos los
/// peers automáticamente, sin RPC ni netcode extra. Como los SoundEvent son 3D,
/// el AudioListener local los posiciona y atenúa por distancia solo.
///
/// Sirve tanto para el player como para el enemigo: asigná en el Inspector solo
/// los sonidos que use ese personaje. Los que dejes en None no suenan (cada
/// método chequea null).
/// </summary>
public class CharacterAudioEmitter : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private SoundEvent footstep;
    [SerializeField] private SoundEvent jump;
    [SerializeField] private SoundEvent dash;

    [Header("Ataques")]
    [SerializeField] private SoundEvent melee;
    [SerializeField] private SoundEvent ranged;
    [SerializeField] private SoundEvent spell01;
    [SerializeField] private SoundEvent spell02;

    [Header("Daño / muerte")]
    [SerializeField] private SoundEvent takeDamage;
    [SerializeField] private SoundEvent death;

    // Métodos llamados por Animation Events. El nombre del "Function" en el clip
    // debe coincidir EXACTO con estos (mayúsculas incluidas).

    public void Footstep()   => footstep?.Play(transform.position);
    public void Jump()       => jump?.Play(transform.position);
    public void Dash()       => dash?.Play(transform.position);
    public void Melee()      => melee?.Play(transform.position);
    public void Ranged()     => ranged?.Play(transform.position);
    public void Spell01()    => spell01?.Play(transform.position);
    public void Spell02()    => spell02?.Play(transform.position);
    public void TakeDamage() => takeDamage?.Play(transform.position);
    public void Death()      => death?.Play(transform.position);
}
