using Fusion;
using Fusion.Sockets;
using Game.CameraSystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Recolecta input cada frame (BeforeUpdate) y lo entrega a Fusion en OnInput.
///
/// Mapa de controles:
///   WASD          → movimiento
///   Space         → salto
///   Shift         → dash
///   F             → interactuar / recoger ítem
///   Mouse izq.    → BasicAttack (melee) + MouseButton0 (protección clicks rápidos)
///   Q             → FirstSkill (ataque a distancia)
///   E             → SecondarySkill
///   Mouse der.    → control de cámara (manejado en CameraController, NO aquí)
///
/// NOTA sobre MouseButton0:
///   Fusion no corre al mismo framerate que Unity. Si el jugador hace un click
///   muy rápido, el botón puede presionarse y soltarse entre dos ticks de Fusion
///   y el input se pierde. _mouseLButtonPressed acumula el click con GetMouseButtonDown
///   y lo mantiene hasta que OnInput lo consume, garantizando que Fusion siempre lo vea.
///
/// NOTA sobre el clic izquierdo sobre UI:
///   El ataque melee (clic izq.) se ignora cuando el puntero está sobre la UI
///   (EventSystem.IsPointerOverGameObject). Sin esto, tocar un slot del inventario
///   también disparaba el ataque y competía con el drag/click de la UI, haciendo
///   que arrastrar items se sintiera peleado. Solo se gatea el clic izquierdo;
///   el input de teclado (movimiento, salto, dash, skills) no se ve afectado.
///
/// NOTA sobre CameraYaw y AttackYaw:
///   Cada cliente envía el yaw de su cámara y el yaw de ataque junto al input para
///   que el servidor pueda calcular movimiento y rotación de ataque correctamente,
///   sin necesidad de tener acceso a la cámara ni al mouse del cliente.
///   AttackYaw se calcula desde la posición real del jugador local (igual que
///   GetMouseDirection en NetCharacterController) para que el ángulo sea correcto
///   independientemente de dónde esté el jugador en el mundo.
/// </summary>
public class NetworkInputManager : SimulationBehaviour, IBeforeUpdate, INetworkRunnerCallbacks
{
    private NetInputPlayer accumulatedInput;
    private bool resetInput;
    private bool _mouseLButtonPressed;
    private CameraController _cachedCameraController;

    // Transform del jugador local — necesario para calcular AttackYaw relativo
    // al jugador. Se asigna desde NetCharacterController.Spawned() cuando el
    // objeto tiene InputAuthority.
    private Transform _localPlayerTransform;

    /// <summary>
    /// Llamado por NetCharacterController al spawnear el jugador local.
    /// Permite calcular AttackYaw relativo a la posición real del jugador.
    /// </summary>
    public void SetLocalPlayer(Transform playerTransform)
    {
        _localPlayerTransform = playerTransform;
    }

    void IBeforeUpdate.BeforeUpdate()
    {
        if (resetInput)
        {
            resetInput = false;
            accumulatedInput = default;
        }

        Keyboard keyboard = Keyboard.current;
        NetworkButtons buttons = default;

        // El clic izquierdo no debe atacar si el puntero está sobre la UI
        // (slots de inventario/crafteo, etc.). Cubre todo el panel porque su
        // Background tiene Raycast Target activo.
        bool pointerOverUI = EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();

        if (!pointerOverUI && Input.GetMouseButtonDown(0))
            _mouseLButtonPressed = true;

        if (keyboard != null)
        {
            Vector2 moveDirection = Vector2.zero;

            if (keyboard.wKey.isPressed) moveDirection += Vector2.up;
            if (keyboard.sKey.isPressed) moveDirection += Vector2.down;
            if (keyboard.aKey.isPressed) moveDirection += Vector2.left;
            if (keyboard.dKey.isPressed) moveDirection += Vector2.right;

            accumulatedInput.Direction = moveDirection;

            accumulatedInput.Buttons.Set(InputButton.Jump, keyboard.spaceKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.Dash, keyboard.shiftKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.Interact, keyboard.fKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.SecondarySkill, keyboard.eKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.FirstSkill, keyboard.qKey.isPressed);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            bool attackHeld = !pointerOverUI && mouse.leftButton.isPressed;
            accumulatedInput.Buttons.Set(InputButton.BasicAttack, attackHeld);
            accumulatedInput.Buttons.Set(InputButton.MouseButton0, _mouseLButtonPressed);
        }

        accumulatedInput.Buttons = new NetworkButtons(accumulatedInput.Buttons.Bits | buttons.Bits);

        // Cache de cámara — la primera vez que esté disponible.
        if (_cachedCameraController == null && Camera.main != null)
            _cachedCameraController = Camera.main.GetComponent<CameraController>();

        // Yaw de la cámara local del cliente. Si todavía no hay cámara, queda en 0
        // (el server caerá a ejes del mundo, mismo comportamiento que antes).
        if (_cachedCameraController != null)
            accumulatedInput.CameraYaw = _cachedCameraController.WorldYaw;

        // Yaw de ataque — dirección desde el jugador hacia el cursor del mouse,
        // proyectada en el plano XZ a la altura del jugador.
        // Equivalente a GetMouseDirection() en NetCharacterController pero calculado
        // acá donde tenemos acceso a Input.mousePosition en tiempo real.
        // Si el jugador local todavía no fue asignado, AttackYaw queda en 0.
        if (Camera.main != null && _localPlayerTransform != null)
        {
            Plane plane = new Plane(Vector3.up, _localPlayerTransform.position);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float dist))
            {
                Vector3 dir = ray.GetPoint(dist) - _localPlayerTransform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    accumulatedInput.AttackYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            }
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        accumulatedInput.Direction.Normalize();
        input.Set(accumulatedInput);
        resetInput = true;
        _mouseLButtonPressed = false;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
            Cursor.visible = true;
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

    void Start() { }
    void Update() { }
}