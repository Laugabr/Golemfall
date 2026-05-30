using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NpcDialogueTrigger : MonoBehaviour
{
    [SerializeField] private NpcData npcData;

    private bool playerInside;

    private void Awake() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;

        if (DialogueController.Instance.IsInDialogue)
            DialogueController.Instance.Advance();
        else if (playerInside)
            DialogueController.Instance.StartDialogue(npcData);
    }
}
