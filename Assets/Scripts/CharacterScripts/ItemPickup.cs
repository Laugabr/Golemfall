using Fusion;
using UnityEngine;
using UnityEngine.SocialPlatforms;

[RequireComponent(typeof(Collider))]
public class ItemPickup : NetworkBehaviour
{
    public ItemData itemData;
    private bool playerInRange = false;
    [HideInInspector] public Vector3 originalPosition;

    void Awake()
    {
        originalPosition = transform.position;
    }

    void Reset()
    {
        // asegurarse que collider sea trigger
        Collider c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            InteractPrompt.Instance?.Show(transform, "F");
        }

if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().HasInputAuthority)
{
    playerInRange = true;
}
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            InteractPrompt.Instance?.Hide();
        }
        
    if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().HasInputAuthority)
    {
        playerInRange = false;
    }
    }

    void Update()
    {
        if (!Object.HasInputAuthority) return; // <--- IMPORTANTÍSIMO

        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            RequestPickup();
        }
    }
    public void ReturnToOriginalPosition()
    {
        transform.position = originalPosition;
        gameObject.SetActive(true);
    }


    public void RequestPickup()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        var playerObj = runner.GetPlayerObject(runner.LocalPlayer);

        PlayerRef localPlayer = Runner.LocalPlayer;


        var inv = playerObj.GetComponent<NetworkInventory>();
        //inv.Server_AddItem(itemData.id, localPlayer); // <- HACÉS LA PETICIÓN AL SERVER

        InteractPrompt.Instance?.Hide();
    }
}
