using UnityEngine;

public class GameInputSimulator : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
         //   Debug.Log("Collected Item");
            TrackEvents.OnTrackEvent?.Invoke(GameEventType.CollectItem, 1, "");
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
          //  Debug.Log("Collected Special Item");
            TrackEvents.OnTrackEvent?.Invoke(GameEventType.CollectSpecialItem, 1, "");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
         //   Debug.Log("Enemy Killed");
            TrackEvents.OnTrackEvent?.Invoke(GameEventType.KillEnemy, 1, "");
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
           // Debug.Log("Talked to NPC");
            TrackEvents.OnTrackEvent?.Invoke(GameEventType.TalkNPC, 1, "");
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
          //  Debug.Log("NPC Killed");
            TrackEvents.OnTrackEvent?.Invoke(GameEventType.KillNPC, 1, "");
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
          //  Debug.Log("Entered Cave");
            TrackEvents.OnTrackEvent?.Invoke(GameEventType.EnterCave, 1, "");
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
           // Debug.Log("Player Died");
            TrackEvents.OnTrackEvent?.Invoke(GameEventType.PlayerDied, 1, "");
        }
    }
}