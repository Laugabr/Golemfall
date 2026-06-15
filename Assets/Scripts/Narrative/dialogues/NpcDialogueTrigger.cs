using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NpcDialogueTrigger : MonoBehaviour
{
    [SerializeField] private NpcData npcData;

    private bool playerInside;
    private NetworkRunner runner;

    private void Awake()
    {
        runner = FindFirstObjectByType<NetworkRunner>();
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        playerInside = false;
    }

    private bool IsLocalPlayer(Collider other)
    {
        if (!other.CompareTag("Player")) return false;
        var no = other.GetComponent<NetworkObject>();
        return no != null && runner != null && no.InputAuthority == runner.LocalPlayer;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;

        if (DialogueController.Instance.IsInDialogue)
        {
            if (DialogueController.Instance.CurrentNpc == npcData)
                DialogueController.Instance.Advance();
        }
        else if (playerInside)
        {
            DialogueController.Instance.StartDialogue(npcData);
        }
    }
}