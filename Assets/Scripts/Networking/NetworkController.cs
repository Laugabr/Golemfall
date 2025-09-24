using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Threading.Tasks;
using Unity.Mathematics;
public class NetworkController : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _lobbyPaneel;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _joinRoomButton;

    [Header("Network")]
    [SerializeField] private NetworkRunner _networkRunner;
    [SerializeField] private NetworkSceneManagerDefault _networkSceneManagerDefault;
    [SerializeField] private NetworkObject _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _players = new Dictionary<PlayerRef, NetworkObject>(); //diccionario que guarda las referencias a los players y a sus objectos, nosotros le agregamos el valor cuando alguien se une

   public void PlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority) return;

        if (_players.Remove(player, out var playerSpawned)) //le hacemos el remove del player spawned, la instancia españneada y agregada al dictionary
        {
            _networkRunner.Despawn(playerSpawned);
        }
    }
 
    public void PlayerJoined(PlayerRef player)
    {
        Debug.Log("On Player Rush");
        _lobbyPaneel.SetActive(false);

        if (!HasStateAuthority) return;

        var playerSpawned = _networkRunner.Spawn(_playerPrefab, new Vector3(UnityEngine.Random.Range(-8, 8), 0, 0), Quaternion.identity, player);

        _players.Add(player, playerSpawned); //le pasamos ala instancia no el prefab
                                             //_players.Add(NetworkRunner runner, NetworkInput input);    
    }
}
