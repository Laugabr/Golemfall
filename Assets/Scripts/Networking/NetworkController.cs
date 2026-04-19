using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Fusion.Sockets;
using UnityEngine.EventSystems;

// Manages network sessions, player spawning, and lobby UI using Fusion
public class NetworkController : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _joinRoomButton;
    [SerializeField] private Transform _spawnPoint;

    //private NetworkProjectConfigAsset _networkConfig;

    [Header("Network")]
    [SerializeField] private NetworkRunner _networkRunner;
    [SerializeField] private NetworkSceneManagerDefault _networkSceneManagerDefault;
    [SerializeField] private NetworkObject _playerPrefab;
    public static NetworkController Instance;
    public Dictionary<PlayerRef, NetworkObject> _players = new Dictionary<PlayerRef, NetworkObject>();

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Assign UI button callbacks
        _createRoomButton.onClick.AddListener(CreateRoom);
        _joinRoomButton.onClick.AddListener(JoinRoom);

    }

    //Gets called On Destroy to debug 
    void OnDestroy()
    {
        Debug.Log("Network runner destroyed " + this);
    }

    // Create a new room as host
    private async void CreateRoom()
    {
        var gameArg = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = "Room_01",
            SceneManager = _networkSceneManagerDefault,
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
        };

        var result = await _networkRunner.StartGame(gameArg);

        if (!result.Ok)
        {
            Debug.LogError(result.ShutdownReason);
            Debug.LogError("Error: " + result.ErrorMessage);
        }
    }

    // Join an existing room as client
    private async void JoinRoom()
    {
        var gameArg = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = "Room_01",
            SceneManager = _networkSceneManagerDefault,
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
        };

        var result = await _networkRunner.StartGame(gameArg);

        if (!result.Ok)
        {
            Debug.LogError(result.ShutdownReason);
            Debug.LogError("Error: " + result.ErrorMessage);
        }
    }

    // ========== Callbacks ==========
    // Called when a player joins the session
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Player joined: " + player);
        if (_lobbyPanel)
        {
            Debug.Log("LobbyPanel encontrado, ocultándolo");
            _lobbyPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("LobbyPanel destruido o no asignado");
        }

        if (!_networkRunner.IsServer)
        {
            Debug.Log(runner.name + " is not server");
            return;
        }
        // Only server spawns players

        // Spawn player prefab for this player
        var playerSpawned = _networkRunner.Spawn(
            _playerPrefab,
            _spawnPoint.position,
            Quaternion.identity,
            player
        );

        _players.Add(player, playerSpawned);

        if (player == _networkRunner.LocalPlayer)
        {
            if (CloudSaveGame.Instance != null)
                CloudSaveGame.Instance.StartGameSave();
        }
    }

    // Called when a player leaves the session
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!_networkRunner.IsServer) return;

        if (_players.Remove(player, out var playerSpawned))
        {
            _networkRunner.Despawn(playerSpawned);
        }
    }

    // ========== Empty Callbacks required==========
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
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}