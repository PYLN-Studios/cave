using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class NetworkRoomPlayerLobby : NetworkBehaviour {
    [Header("UI")]
    [SerializeField] private GameObject lobbyUI = null;
    [SerializeField] private Transform playerListContainer = null;
    [SerializeField] private PlayerListItem playerListItemPrefab = null;
    [SerializeField] private Button startGameButton = null;

    [SyncVar(hook = nameof(HandleDisplayNameChanged))]
    public string DisplayName = "Loading...";

    [SyncVar(hook = nameof(HandleReadyStatusChanged))]
    public bool IsReady = false;

    private bool isLeader;
    public bool IsLeader {
        set {
            isLeader = value;
            if (startGameButton != null)
                startGameButton.gameObject.SetActive(value);
        }
    }

    private NetworkManagerLobby room;
    private NetworkManagerLobby Room {
        get {
            if (room != null) { return room; }
            return room = NetworkManager.singleton as NetworkManagerLobby;
        }
    }

    public override void OnStartAuthority() {
        CmdSetDisplayName(PlayerNameInput.DisplayName);
        if (lobbyUI != null)
            lobbyUI.SetActive(true);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(CmdStartGame);
    }

    public override void OnStartClient() {
        if (Room != null)
            Room.RoomPlayers.Add(this);
        UpdateDisplay();
    }

    public override void OnStopClient() {
        if (Room != null)
            Room.RoomPlayers.Remove(this);
        UpdateDisplay();
    }

    public void HandleReadyStatusChanged(bool oldValue, bool newValue) => UpdateDisplay();
    public void HandleDisplayNameChanged(string oldValue, string newValue) => UpdateDisplay();

    private void UpdateDisplay() {
        if (Room == null) return;

        if (!isOwned) {
            foreach (var player in Room.RoomPlayers) {
                if (player.isOwned) {
                    player.UpdateDisplay();
                    break;
                }
            }
            return;
        }

        if (playerListContainer == null) return;

        // Clear existing list items
        foreach (Transform child in playerListContainer) {
            Destroy(child.gameObject);
        }

        // Create new list items for each player
        foreach (var player in Room.RoomPlayers) {
            PlayerListItem item = Instantiate(playerListItemPrefab, playerListContainer);
            item.Setup(player.DisplayName, player.IsReady, player.isOwned);
        }
    }

    public void HandleReadyToStart(bool readyToStart) {
        if (!isLeader) return;
        if (startGameButton != null)
            startGameButton.interactable = readyToStart;
    }

    [Command]
    private void CmdSetDisplayName(string displayName) {
        DisplayName = displayName;
    }

    [Command]
    public void CmdReadyUp() {
        IsReady = !IsReady;
        Room.NotifyPlayersOfReadyState();
    }

    [Command]
    public void CmdStartGame() {
        if (Room.RoomPlayers[0].connectionToClient != connectionToClient) return;
        Room.StartGame();
    }
}