using UnityEngine;

public class GameInputSimulator : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("Collected Item");
            TrackEvents.OnTrackEvent?.Invoke("Collect_Item", 1);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Enemy Killed");
            TrackEvents.OnTrackEvent?.Invoke("Kill_Enemy", 1);
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("Talked to NPC");
            TrackEvents.OnTrackEvent?.Invoke("Talk_NPC", 1);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("NPC Killed");
            TrackEvents.OnTrackEvent?.Invoke("Kill_NPC", 1);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Entered Cave");
            TrackEvents.OnTrackEvent?.Invoke("Enter_Cave", 1);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("Player Died");
            TrackEvents.OnTrackEvent?.Invoke("Player_Died", 1);
        }
    }
}
