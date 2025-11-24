using UnityEngine;

public class AbilityHolder : MonoBehaviour
{
    public Ability ability;
    private float cooldownTime;
    private float activeTime;

    AbilityState state = AbilityState.Ready;

    public KeyCode key;

    void Update()
    {
        switch (state)
        {
            case AbilityState.Ready:
                if (Input.GetKey(key))
                {
                    if (ability == null)
                    {
                        Debug.LogError("No ability assigned to " + gameObject.name);
                        return;
                    }

                    ability.Activate(gameObject);
                    state = AbilityState.Active;
                    activeTime = ability.activeTime;
                }
                break;
            case AbilityState.Active:
                if (activeTime > 0)
                {
                    activeTime -= Time.deltaTime;
                } 
                else
                {
                    state = AbilityState.Cooldown;
                    cooldownTime = ability.cooldownTime;
                }       
                break;
            case AbilityState.Cooldown:
                if (cooldownTime > 0)
                {
                    cooldownTime -= Time.deltaTime;
                }
                else
                {
                    state = AbilityState.Ready;
                }
                break;
        }

    }

}
enum AbilityState
{
    Ready,
    Active,
    Cooldown
}

