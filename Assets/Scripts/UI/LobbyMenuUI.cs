using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System;
using System.Collections.Generic;

// Cerebro del lobby: navega entre las 3 vistas (Menú / Crear / Unirse),
// valida el nombre de sala y dispara las acciones del NetworkController.
// Presentación pura: no toca red directo, solo llama al NetworkController.
public class LobbyMenuUI : MonoBehaviour
{
    [Header("Vistas")]
    [SerializeField] private GameObject _menuView;
    [SerializeField] private GameObject _createRoomView;
    [SerializeField] private GameObject _joinRoomView;

    [Header("Botones del Menú")]
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _joinRoomButton;      // se muestra solo si hay salas activas
    [SerializeField] private Button _singlePlayerButton;

    [Header("Vista Crear Sala")]
    [SerializeField] private TMP_InputField _roomNameInput;
    [SerializeField] private Button _confirmCreateButton;
    [SerializeField] private Button _createBackButton;
    [SerializeField] private TMP_Text _statusText;

    [Header("Vista Unirse a Sala")]
    [SerializeField] private Transform _roomListContent;   // el "Content" del ScrollView
    [SerializeField] private RoomListEntry _roomEntryPrefab;
    [SerializeField] private Button _joinBackButton;

    // Pool de filas reutilizables: se crean una vez y se reciclan entre refreshes.
    private readonly List<RoomListEntry> _pooledEntries = new List<RoomListEntry>();

    private void Start()
    {
        // Wireo de botones
        _createRoomButton.onClick.AddListener(ShowCreateRoom);
        _joinRoomButton.onClick.AddListener(ShowJoinRoom);
        _singlePlayerButton.onClick.AddListener(OnSinglePlayer);
        _confirmCreateButton.onClick.AddListener(OnConfirmCreate);
        _createBackButton.onClick.AddListener(ShowMenu);
        _joinBackButton.onClick.AddListener(ShowMenu);

        // Suscripción a los eventos de red
        if (NetworkController.Instance != null)
        {
            NetworkController.Instance.SessionListUpdated += OnSessionListUpdated;
            NetworkController.Instance.RoomActionFailed += OnRoomActionFailed;
        }

        ShowMenu();
        // Estado inicial del botón Unirse según lo que haya ahora.
        UpdateJoinButtonVisibility(NetworkController.Instance != null
            ? NetworkController.Instance.CurrentSessions
            : new List<SessionInfo>());
    }

    private void OnDestroy()
    {
        if (NetworkController.Instance != null)
        {
            NetworkController.Instance.SessionListUpdated -= OnSessionListUpdated;
            NetworkController.Instance.RoomActionFailed -= OnRoomActionFailed;
        }
    }

    // ========== Navegación entre vistas ==========

    private void ShowMenu()
    {
        _menuView.SetActive(true);
        _createRoomView.SetActive(false);
        _joinRoomView.SetActive(false);
    }

    private void ShowCreateRoom()
    {
        _menuView.SetActive(false);
        _createRoomView.SetActive(true);
        _joinRoomView.SetActive(false);

        _statusText.text = "";
        _roomNameInput.text = "";
        _roomNameInput.ActivateInputField(); // foco directo para escribir
    }

    private void ShowJoinRoom()
    {
        _menuView.SetActive(false);
        _createRoomView.SetActive(false);
        _joinRoomView.SetActive(true);

        RebuildRoomList();
    }

    // ========== Acciones ==========

    private void OnConfirmCreate()
    {
        string roomName = _roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            _statusText.text = "Ingresá un nombre para la sala.";
            return;
        }

        if (RoomNameExists(roomName))
        {
            _statusText.text = "Ese nombre ya existe, probá con otro.";
            return;
        }

        _statusText.text = "Creando sala...";
        NetworkController.Instance.CreateRoom(roomName);
        // Si todo va bien, NetworkController oculta el LobbyPanel solo.
        // Si falla, llega por OnRoomActionFailed y se muestra el error.
    }

    private void OnSinglePlayer()
    {
        NetworkController.Instance.StartSinglePlayer();
    }

    private void OnRowJoinClicked(string sessionName)
    {
        NetworkController.Instance.JoinRoom(sessionName);
    }

    // ========== Reacción a la red ==========

    private void OnSessionListUpdated(IReadOnlyList<SessionInfo> sessions)
    {
        UpdateJoinButtonVisibility(sessions);

        if (_joinRoomView.activeSelf)
            RebuildRoomList();
    }

    private void OnRoomActionFailed(string message)
    {
        // El error se muestra en el statusText (visible en la vista Crear).
        if (_statusText != null)
            _statusText.text = message;
        Debug.LogWarning("[LobbyMenuUI] " + message);
    }

    // ========== Helpers ==========

    // Muestra u oculta el botón Unirse según haya o no salas visibles.
    private void UpdateJoinButtonVisibility(IReadOnlyList<SessionInfo> sessions)
    {
        bool haySalas = false;
        foreach (var s in sessions)
        {
            if (s.IsVisible)
            {
                haySalas = true;
                break;
            }
        }
        _joinRoomButton.interactable = haySalas;
    }

    // Chequeo de duplicados contra la lista cacheada (case-insensitive).
    private bool RoomNameExists(string roomName)
    {
        foreach (var s in NetworkController.Instance.CurrentSessions)
        {
            if (s.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // Reconstruye la lista reutilizando filas (Opción B): crea solo las que
    // falten, reutiliza las existentes y apaga las sobrantes. Sin destruir.
    private void RebuildRoomList()
    {
        // 1. Junta las salas visibles.
        var visibles = new List<SessionInfo>();
        foreach (var session in NetworkController.Instance.CurrentSessions)
        {
            if (session.IsVisible)
                visibles.Add(session);
        }

        // 2. Si faltan filas en el pool, crea solo las que falten (nunca destruye).
        while (_pooledEntries.Count < visibles.Count)
        {
            var entry = Instantiate(_roomEntryPrefab);
            entry.transform.SetParent(_roomListContent, false); // false = adopta el layout, no la escala del prefab
            _pooledEntries.Add(entry);
        }

        // 3. Reutiliza las filas: activa y configura las necesarias, apaga las sobrantes.
        for (int i = 0; i < _pooledEntries.Count; i++)
        {
            if (i < visibles.Count)
            {
                _pooledEntries[i].gameObject.SetActive(true);
                _pooledEntries[i].Setup(visibles[i], OnRowJoinClicked);
            }
            else
            {
                _pooledEntries[i].gameObject.SetActive(false);
            }
        }

        // 4. Content Size Fitter + poblado en runtime necesita este rebuild para
        //    resolver el cálculo multi-pase en este frame (sin filas encimadas).
        LayoutRebuilder.ForceRebuildLayoutImmediate(_roomListContent as RectTransform);
    }
}