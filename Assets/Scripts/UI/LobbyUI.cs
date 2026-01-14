using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class LobbyUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button readyButton = null;
    [SerializeField] private Button startButton = null;
    [SerializeField] private Button leaveButton = null;

    private NetworkRoomPlayerLobby roomPlayer;

    private void Start()
    {
        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClicked);

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnLeaveClicked);
    }

    public void SetRoomPlayer(NetworkRoomPlayerLobby player)
    {
        roomPlayer = player;
    }

    private void OnReadyClicked()
    {
        if (roomPlayer == null)
        {
            // Find the local player's NetworkRoomPlayerLobby
            roomPlayer = NetworkClient.localPlayer?.GetComponent<NetworkRoomPlayerLobby>();
        }

        if (roomPlayer != null)
        {
            roomPlayer.CmdReadyUp();
        }
    }

    private void OnStartClicked()
    {
        if (roomPlayer == null)
        {
            roomPlayer = NetworkClient.localPlayer?.GetComponent<NetworkRoomPlayerLobby>();
        }

        if (roomPlayer != null)
        {
            roomPlayer.CmdStartGame();
        }
    }

    private void OnLeaveClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            // Host - stop hosting
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            // Client - disconnect
            NetworkManager.singleton.StopClient();
        }
    }
}
