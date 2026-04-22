using UnityEngine;

public class ArenaManager : MonoBehaviour
{
    [SerializeField] private ArenaTrigger trigger;
    [SerializeField] private GameObject boss;

    private void OnEnable()
    {
        trigger.OnAllPlayersInside += StartFight;
    }

    private void OnDisable()
    {
        trigger.OnAllPlayersInside -= StartFight;
    }

    void StartFight()
    {
        Debug.Log("[ArenaManager] COMIENZA LA PELEA");

        boss.SetActive(true);

       
    }
}