using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

//Simulation behaviour to make it work outside of a networkbehaviour
public class NetworkInputManager : SimulationBehaviour, IBeforeUpdate, INetworkRunnerCallbacks
{
    private NetInputPlayer accumulatedInput;
    private bool resetInput;
    private bool _mouseLButtonPressed;
    private bool _mouseRButtonPressed;

    void IBeforeUpdate.BeforeUpdate() //same as normal udpate but executed before fusions network loop
    {
        if (resetInput)
        {
            resetInput = false;
            accumulatedInput = default;
        }

        Keyboard keyboard = Keyboard.current;
        NetworkButtons buttons = default;

        if (Input.GetMouseButtonDown(0))
        {
            _mouseLButtonPressed = true;
        }

        if (Input.GetMouseButtonDown(1))
        {
            _mouseRButtonPressed = true;
        }



        if (keyboard != null)
        {
            Vector2 moveDirection = Vector2.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                moveDirection += Vector2.up;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                moveDirection += Vector2.down;
            }
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                moveDirection += Vector2.left;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                moveDirection += Vector2.right;
            }

            accumulatedInput.Buttons.Set(NetInputPlayer.MOUSE_BUTTON_0, _mouseLButtonPressed);
            accumulatedInput.Buttons.Set(NetInputPlayer.MOUSE_BUTTON_1, _mouseRButtonPressed);

            accumulatedInput.Direction += moveDirection;

            accumulatedInput.Buttons.Set(InputButton.Jump, keyboard.spaceKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.Dash, keyboard.shiftKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.Interact, keyboard.fKey.isPressed);
            accumulatedInput.Buttons.Set(InputButton.SecondarySkill, keyboard.eKey.isPressed);
        }

            Mouse mouse = Mouse.current;
            if(mouse != null)
            {
                accumulatedInput.Buttons.Set(InputButton.BasicAttack, mouse.leftButton.isPressed);
                accumulatedInput.Buttons.Set(InputButton.FirstSkill, mouse.rightButton.isPressed);
            }
            
    }
 
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        accumulatedInput.Direction.Normalize();
        input.Set(accumulatedInput);
        resetInput = true;
        _mouseRButtonPressed = false;
        _mouseLButtonPressed = false;

    }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            //Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true;
        }
    }

    public void OnConnectedToServer(NetworkRunner runner) { }


    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }


    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }



    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }


    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    //called when the conection fails or we are disconected    
    public async void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (shutdownReason == ShutdownReason.DisconnectedByPluginLogic)
        {
            //await FindFirstObjectByType<menucone>()
        }
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }
}
