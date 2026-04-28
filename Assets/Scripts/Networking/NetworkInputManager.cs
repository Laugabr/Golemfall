using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
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
///   Mouse izq.    → BasicAttack (melee)
///   Q             → FirstSkill (ataque a distancia)
///   E             → SecondarySkill
///   Mouse der.    → control de cámara (manejado en CameraController, NO aquí)
/// </summary>
public class NetworkInputManager : SimulationBehaviour, IBeforeUpdate, INetworkRunnerCallbacks
{
    private NetInputPlayer accumulatedInput;
    private bool resetInput;
    private bool _mouseLButtonPressed;

    void IBeforeUpdate.BeforeUpdate()
    {
        if (resetInput)
        {
            resetInput = false;
            accumulatedInput = default;
        }

        Keyboard keyboard = Keyboard.current;
        NetworkButtons buttons = default;

        // Mouse izquierdo — BasicAttack (melee).
        // Solo registramos el press, no el hold, para ataques discretos.
        if (Input.GetMouseButtonDown(0))
            _mouseLButtonPressed = true;

        // NOTA: Mouse derecho (botón 1) se dejó de capturar aquí intencionalmente.
        // El botón derecho es ahora exclusivo de la rotación de cámara (CameraController).

        if (keyboard != null)
        {
            // ── Movimiento ──────────────────────────────────────────────────────
            Vector2 moveDirection = Vector2.zero;

            if (keyboard.wKey.isPressed) moveDirection += Vector2.up;
            if (keyboard.sKey.isPressed) moveDirection += Vector2.down;
            if (keyboard.aKey.isPressed) moveDirection += Vector2.left;
            if (keyboard.dKey.isPressed) moveDirection += Vector2.right;

            accumulatedInput.Direction += moveDirection;

            // ── Botones de acción ────────────────────────────────────────────────
            accumulatedInput.Buttons.Set(InputButton.Jump,          keyboard.spaceKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.Dash,          keyboard.shiftKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.Interact,      keyboard.fKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.SecondarySkill,keyboard.eKey.isPressed);

            // Q → FirstSkill (ataque a distancia)
            accumulatedInput.Buttons.Set(InputButton.FirstSkill,    keyboard.qKey.isPressed);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            // Mouse izquierdo → BasicAttack (melee).
            // Usamos GetMouseButtonDown (acumulado arriba) para disparar una sola vez por click.
            accumulatedInput.Buttons.Set(NetInputPlayer.MOUSE_BUTTON_0, _mouseLButtonPressed);
            accumulatedInput.Buttons.Set(InputButton.BasicAttack, mouse.leftButton.isPressed);
        }

        accumulatedInput.Buttons = new NetworkButtons(accumulatedInput.Buttons.Bits | buttons.Bits);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        accumulatedInput.Direction.Normalize();
        input.Set(accumulatedInput);
        resetInput         = true;
        _mouseLButtonPressed = false;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
            Cursor.visible = true;
    }

    // ── Empty callbacks requeridos por INetworkRunnerCallbacks ──────────────────

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