using Game.CameraSystem;
using UnityEngine;

public class BossCameraZone : MonoBehaviour
{
    [SerializeField] private string presetName = "Boss";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var cam = Camera.main.GetComponent<CameraController>();
        cam?.TransitionToPreset(presetName);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var cam = Camera.main.GetComponent<CameraController>();
        cam?.TransitionToPreset("Normal"); // vuelve al preset normal al salir
    }
}