using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System;
using Fusion.Sockets;

// Manages network sessions, player spawning, and lobby/session-list using Fusion
public class NetworkController : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private GameObject _Hud;

    [Header("Network")]
    [SerializeField] private NetworkRunner _networkRunner;
    [SerializeField] private NetworkSceneManagerDefault _networkSceneManagerDefault;
    [SerializeField] private NetworkObject _playerPrefab;

    [Header("Room Settings")]
    [SerializeField] private int _maxPlayersPerRoom = 5;

    // Nombre de lobby explícito: host y buscador DEBEN usar el mismo,
    // sino Fusion los manda a lobbies distintos por GameMode y no se ven.
    private const string LOBBY_NAME = "GolemfallLobby";

    public static NetworkController Instance;
    public Dictionary<PlayerRef, NetworkObject> _players = new Dictionary<PlayerRef, NetworkObject>();

    // Lista de salas cacheada, la consume la UI para armar la lista y validar duplicados.
    public IReadOnlyList<SessionInfo> CurrentSessions => _currentSessions;
    private List<SessionInfo> _currentSessions = new List<SessionInfo>();

    // Eventos hacia la UI (no toca botones directo -> SRP).
    public event Action<IReadOnlyList<SessionInfo>> SessionListUpdated;
    public event Action<string> RoomActionFailed;

    // Candado anti doble-click / anti StartGame en vuelo.
    private bool _isConnecting;

    void Awake()
    {
        Instance = this;
    }

    private async void Start()
    {
        // Entramos al lobby custom (NO arranca partida): habilita recibir OnSessionListUpdated.
        var result = await _networkRunner.JoinSessionLobby(SessionLobby.Custom, LOBBY_NAME);

        if (!result.Ok)
        {
            Debug.LogError("[NetworkController] JoinSessionLobby falló: " + result.ShutdownReason + " / " + result.ErrorMessage);
            RoomActionFailed?.Invoke("No se pudo conectar al lobby. Revisá tu conexión.");
        }
    }

    void OnDestroy()
    {
        Debug.Log("Network runner destroyed " + this);
    }

    // ========== Acciones públicas (las llama la UI) ==========

    // Crea una sala nueva como host. El nombre lo valida la UI (duplicados) antes de llamar.
    public async void CreateRoom(string sessionName)
    {
        if (_isConnecting) return;
        _isConnecting = true;

        var gameArg = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            CustomLobbyName = LOBBY_NAME,   // publica la sala en el MISMO lobby que escucha el buscador
            PlayerCount = _maxPlayersPerRoom,
            SceneManager = _networkSceneManagerDefault,
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
        };

        var result = await _networkRunner.StartGame(gameArg);
        HandleStartResult(result, "No se pudo crear la sala.");
    }

    // Se une a una sala existente como cliente.
    public async void JoinRoom(string sessionName)
    {
        if (_isConnecting) return;
        _isConnecting = true;

        var gameArg = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            CustomLobbyName = LOBBY_NAME,   // consistente con el resto (unir por nombre igual funciona)
            SceneManager = _networkSceneManagerDefault,
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
        };

        var result = await _networkRunner.StartGame(gameArg);
        HandleStartResult(result, "No se pudo unir a la sala.");
    }

    public async void StartSinglePlayer()
    {
        if (_isConnecting) return;
        _isConnecting = true;

        var gameArg = new StartGameArgs()
        {
            GameMode = GameMode.Single,
            SessionName = "SinglePlayer_" + Guid.NewGuid(),
            SceneManager = _networkSceneManagerDefault,
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
        };

        var result = await _networkRunner.StartGame(gameArg);
        HandleStartResult(result, "No se pudo iniciar el modo un jugador.");
    }

    // Revierte el candado si falló, para que el runner NO quede colgado y la UI se reactive.
    private void HandleStartResult(StartGameResult result, string userMessage)
    {
        if (result.Ok)
        {
            if (_lobbyPanel)
                _lobbyPanel.SetActive(false);
            return;
        }

        Debug.LogError("[NetworkController] " + result.ShutdownReason + " / " + result.ErrorMessage);
        _isConnecting = false;
        RoomActionFailed?.Invoke(userMessage);
    }

    // ========== Callbacks ==========

    // Se llama cada vez que Photon actualiza la lista de salas del lobby.
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _currentSessions = sessionList;
        SessionListUpdated?.Invoke(_currentSessions);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Player joined: " + player);
        if (_lobbyPanel)
            _lobbyPanel.SetActive(false);
        else
            Debug.LogWarning("LobbyPanel destruido o no asignado");

        // El spawn de jugadores y el guardado los maneja solo el servidor.
        if (_networkRunner.IsServer)
        {
            var playerSpawned = _networkRunner.Spawn(
                _playerPrefab,
                _spawnPoint.position,
                Quaternion.identity,
                player
            );

            _players.Add(player, playerSpawned);

            int playerIndex = _players.Count - 1;
            var colorSetting = playerSpawned.GetComponentInChildren<PlayerColorSetting>(true);
            if (colorSetting != null)
                colorSetting.PlayerIndex = playerIndex;
            else
                Debug.LogWarning("[NetworkController] No se encontró PlayerColorSetting en el prefab.");

            PlayerRegistry.Register(playerSpawned.transform);
            Debug.Log($"[NetworkController] Player Registrado. Total: {PlayerRegistry.Players.Count}");

            if (player == _networkRunner.LocalPlayer && CloudSaveGame.Instance != null)
                CloudSaveGame.Instance.StartGameSave();
            if (player == runner.LocalPlayer && _Hud != null)
                _Hud.SetActive(true);
        }

        // El HUD se activa para el jugador local, sea host o cliente.
        if (player == runner.LocalPlayer && _Hud != null)
            _Hud.SetActive(true);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!_networkRunner.IsServer) return;

        if (_players.Remove(player, out var playerSpawned))
        {
            _networkRunner.Despawn(playerSpawned);
        }

        var obj = runner.GetPlayerObject(player);
        if (obj != null)
            FakeProjectileRegistry.Clear(obj.Id.Raw);
    }

    // ========== Empty Callbacks required ==========
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}