using Fusion;
using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class ProximityInteractor : MonoBehaviour
{
    public static event Action<ProximityInteractor> OnPlayerEntered;
    public static event Action<ProximityInteractor> OnPlayerExited;

    [SerializeField] private string promptText = "F";
    public string PromptText => promptText;

    private NetworkRunner runner;

    private void Awake()
    {
        runner = FindFirstObjectByType<NetworkRunner>();
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var no = other.GetComponent<NetworkObject>();
        if (no == null || no.InputAuthority != runner.LocalPlayer) return;
        OnPlayerEntered?.Invoke(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var no = other.GetComponent<NetworkObject>();
        if (no == null || no.InputAuthority != runner.LocalPlayer) return;
        OnPlayerExited?.Invoke(this);
    }
}
