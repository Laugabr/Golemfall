using Fusion;
using UnityEngine;

/// <summary>
/// Maneja la recolección de items cercanos con la tecla F (InputButton.Interact).
///
/// ÚNICA fuente de verdad para el pickup: NetCharacterController YA NO llama a
/// TryPickUp() — antes lo hacía y quedó duplicado con este script, generando una
/// carrera de condiciones (dos detecciones de flanco corriendo en paralelo) que
/// se comportaba distinto según la máquina.
///
/// Corre en HOST (StateAuthority) y en el CLIENTE dueño (InputAuthority), igual
/// que el patrón ya usado para Melee/Jump en NetCharacterAnimator — sin gates
/// adicionales de autoridad, para que el host escriba el tick confirmado y los
/// proxies lo vean sincronizado en Render().
///
/// La animación SOLO se dispara si currentInteractor != null (hay un item cerca).
/// Esto evita que F "en el aire" (por ejemplo hablando con un NPC) interrumpa
/// otras animaciones.
/// </summary>
public class CharacterPickUp : NetworkBehaviour
{
    [SerializeField] private NetCharacterAnimator characterAnimator;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private ProximityInteractor currentInteractor;

    [Networked] private NetworkButtons PreviousButtons { get; set; }

    private void Awake()
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<NetCharacterAnimator>();
        if (characterAnimator == null)
            characterAnimator = GetComponentInParent<NetCharacterAnimator>();
        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<NetCharacterAnimator>();

        if (characterAnimator == null)
            Debug.LogWarning($"[CharacterPickUp] No se encontró NetCharacterAnimator en {name}. La animación de pickup no va a reproducirse.");
    }

    private void OnEnable()
    {
        ProximityInteractor.OnPlayerEntered += HandleEntered;
        ProximityInteractor.OnPlayerExited += HandleExited;
    }

    private void OnDisable()
    {
        ProximityInteractor.OnPlayerEntered -= HandleEntered;
        ProximityInteractor.OnPlayerExited -= HandleExited;
    }

    private void HandleEntered(ProximityInteractor interactor)
    {
        currentInteractor = interactor;
    }

    private void HandleExited(ProximityInteractor interactor)
    {
        if (currentInteractor == interactor)
            currentInteractor = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetInputPlayer input)) return;

        bool pressed = input.Buttons.WasPressed(PreviousButtons, InputButton.Interact);

        if (pressed && debugLogs)
            Debug.Log($"[PICKUP-DEBUG] F detectado en {Object} | HasStateAuthority={HasStateAuthority} | HasInputAuthority={HasInputAuthority} | currentInteractor null? {currentInteractor == null}");

        // Gate clave: solo actúa si hay un item cerca. Si no hay nada, F no hace
        // nada acá (puede seguir usándose para diálogo con NPC en otro sistema).
        if (pressed && currentInteractor != null)
        {
            TryPickUp();
        }

        PreviousButtons = input.Buttons;
    }

    public void TryPickUp()
    {
        if (currentInteractor == null) return;

        var item = currentInteractor.GetComponent<PickableItem>();
        if (item == null) return;

        item.Rpc_Collect(Object);
        TrackEvents.OnTrackEvent?.Invoke(GameEventType.CollectItem, 1, item.Item.missionKey);

        if (debugLogs)
            Debug.Log($"[PICKUP-DEBUG] TryPickUp ejecutó Rpc_Collect en {Object} | characterAnimator null? {characterAnimator == null}");

        if (characterAnimator != null)
            characterAnimator.TriggerPickupAnimation();

        InteractPrompt.Instance?.Hide();
        MessageManager.Instance?.Show("Item recolectado");
        currentInteractor = null;
    }
}