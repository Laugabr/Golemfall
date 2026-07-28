using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System;

// Una fila de la lista de salas. Presentación pura: muestra "nombre — X/5"
// y avisa por callback cuando se la clickea. No conoce a NetworkController.
public class RoomListEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Button _joinButton;

    private string _sessionName;
    private Action<string> _onJoinClicked;

    // La llama LobbyMenuUI al instanciar/reciclar la fila.
    public void Setup(SessionInfo session, Action<string> onJoinClicked)
    {
        _sessionName = session.Name;
        _onJoinClicked = onJoinClicked;

        bool isFull = session.PlayerCount >= session.MaxPlayers;
        bool isJoinable = session.IsOpen && session.IsVisible && !isFull;

        _label.text = isFull
            ? $"{session.Name} — {session.PlayerCount}/{session.MaxPlayers} (llena)"
            : $"{session.Name} — {session.PlayerCount}/{session.MaxPlayers}";

        _joinButton.interactable = isJoinable;

        _joinButton.onClick.RemoveAllListeners();
        _joinButton.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        _onJoinClicked?.Invoke(_sessionName);
    }
}
