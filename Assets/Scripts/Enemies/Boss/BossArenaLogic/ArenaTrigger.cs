using System.Collections.Generic;
using UnityEngine;

public class ArenaTrigger : MonoBehaviour
{
    private HashSet<Transform> playersInside = new();

    public System.Action OnAllPlayersInside;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInside.Add(other.transform);

        Debug.Log($"[Arena] Entra: {other.name}");

        CheckAllInside();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInside.Remove(other.transform);

        Debug.Log($"[Arena] Sale: {other.name}");
    }

    void CheckAllInside()
    {
        int totalPlayers = PlayerRegistry.Players.Count;
        int inside = playersInside.Count;

        Debug.Log($"[Arena] Dentro: {inside}/{totalPlayers}");

        if (totalPlayers > 0 && inside == totalPlayers)
        {
            Debug.Log("[Arena] TODOS DENTRO → iniciar combate");
            OnAllPlayersInside?.Invoke();
        }
    }
}
