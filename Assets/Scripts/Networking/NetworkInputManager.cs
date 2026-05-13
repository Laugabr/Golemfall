using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Game.CameraSystem;
using UnityEngine;
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
/// NOTA sobre CameraYaw:
///   Cada cliente envía el yaw de su propia cámara junto al input para que
///   el server pueda transformar Direction al espacio mundo correctamente
///   sin necesidad de tener acceso a la cámara local del cliente.
/// </summary>
public class NetworkInputManager : SimulationBehaviour, IBeforeUpdate, INetworkRunnerCallbacks
{
    private NetInputPlayer accumulatedInput;
    private bool resetInput;
    private bool _mouseLButtonPressed;
    private CameraController _cachedCameraController;

    void IBeforeUpdate.BeforeUpdate()
    {
        if (resetInput)
        {
            resetInput = false;
            accumulatedInput = default;
        }

        Keyboard keyboard = Keyboard.current;
        NetworkButtons buttons = default;

        if (Input.GetMouseButtonDown(0))
            _mouseLButtonPressed = true;

        if (keyboard != null)
        {
            Vector2 moveDirection = Vector2.zero;

            if (keyboard.wKey.isPressed) moveDirection += Vector2.up;
            if (keyboard.sKey.isPressed) moveDirection += Vector2.down;
            if (keyboard.aKey.isPressed) moveDirection += Vector2.left;
            if (keyboard.dKey.isPressed) moveDirection += Vector2.right;

            accumulatedInput.Direction = moveDirection;

            accumulatedInput.Buttons.Set(InputButton.Jump,           keyboard.spaceKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.Dash,           keyboard.shiftKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.Interact,       keyboard.fKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.SecondarySkill, keyboard.eKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.FirstSkill,     keyboard.qKey.isPressed);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            accumulatedInput.Buttons.Set(InputButton.BasicAttack,  mouse.leftButton.isPressed);
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
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        accumulatedInput.Direction.Normalize();
        input.Set(accumulatedInput);
        resetInput           = true;
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

    public async void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

    void Start() { }
    void Update() { }
}