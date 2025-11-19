using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Fusion.Sockets;
using UnityEngine.EventSystems;

public class NetworkController : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _joinRoomButton;

    [Header("Network")]
    [SerializeField] private NetworkRunner _networkRunner;
    [SerializeField] private NetworkSceneManagerDefault _networkSceneManagerDefault;
    [SerializeField] private NetworkObject _playerPrefab;

    private Dictionary<PlayerRef, NetworkObject> _players = new Dictionary<PlayerRef, NetworkObject>();

    private void Start()
    {
        _createRoomButton.onClick.AddListener(CreateRoom);
        _joinRoomButton.onClick.AddListener(JoinRoom);
    }

    private async void CreateRoom()
    {
        var gameArg = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = "Room_01",
            SceneManager = _networkSceneManagerDefault,
            Scene = SceneRef.FromIndex(0)

        };

        var result = await _networkRunner.StartGame(gameArg);

        if (!result.Ok)
        {
            Debug.LogError(result.ShutdownReason);
            Debug.LogError("Error: " + result.ErrorMessage);
        }
    }

    private async void JoinRoom()
    {
        var gameArg = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = "Room_01",
            SceneManager = _networkSceneManagerDefault,
            Scene = SceneRef.FromIndex(0)

        };

        var result = await _networkRunner.StartGame(gameArg);

        if (!result.Ok)
        {
            Debug.LogError(result.ShutdownReason);
            Debug.LogError("Error: " + result.ErrorMessage);
        }
    }

    // ========== Callbacks ==========
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Player joined: " + player);
        _lobbyPanel.SetActive(false);

        if (!_networkRunner.IsServer) return;

        var playerSpawned = _networkRunner.Spawn(
            _playerPrefab,
            new Vector3(0, 15, 0),
            Quaternion.identity,
            player
        );

        var playerSpawnedPlayerStats = playerSpawned.GetComponent<PlayerStats>();
        
        playerSpawnedPlayerStats.Initialize();

        _players.Add(player, playerSpawned);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!_networkRunner.IsServer) return;

        if (_players.Remove(player, out var playerSpawned))
        {
            _networkRunner.Despawn(playerSpawned);
        }
    }

    // ========== Callbacks vacíos que no usás, pero necesarios para compilar ==========
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