using System.Collections.Generic;
using UnityEngine;

public static class PlayerRegistry
{
    public static List<Transform> Players = new();

    public static void Register(Transform player)
    {
        if (!Players.Contains(player))
        {
            Players.Add(player);
            Debug.Log("[Registry] Player registrado");
        }
    }

    public static void Unregister(Transform player)
    {
        if (Players.Contains(player))
        {
            Players.Remove(player);
        }
    }
}