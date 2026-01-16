using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel = null;
    [SerializeField] private GameObject joinPanel = null;

    [Header("Main Panel Buttons")]
    [SerializeField] private Button hostButton = null;
    [SerializeField] private Button joinButton = null;
    [SerializeField] private Button quitButton = null;

    [Header("Join Panel")]
    [SerializeField] private TMP_InputField ipAddressInput = null;
    [SerializeField] private Button connectButton = null;
    [SerializeField] private Button backButton = null;

    private NetworkManagerLobby networkManager;

    private void Start()
    {
        networkManager = NetworkManager.singleton as NetworkManagerLobby;

        // Setup button listeners
        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostClicked);

        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        // Set default IP
        if (ipAddressInput != null)
            ipAddressInput.text = "localhost";

        // Subscribe to network events
        NetworkManagerLobby.OnClientConnected += OnClientConnected;
        NetworkManagerLobby.OnClientDisconnected += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        NetworkManagerLobby.OnClientConnected -= OnClientConnected;
        NetworkManagerLobby.OnClientDisconnected -= OnClientDisconnected;
    }

    private void OnHostClicked()
    {
        networkManager.StartHost();
    }

    private void OnJoinClicked()
    {
        mainPanel.SetActive(false);
        joinPanel.SetActive(true);
    }

    private void OnConnectClicked()
    {
        string ipAddress = ipAddressInput.text;
        networkManager.networkAddress = ipAddress;
        networkManager.StartClient();
    }

    private void OnBackClicked()
    {
        joinPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnClientConnected()
    {
        // Connection successful - lobby UI will be activated by NetworkRoomPlayerLobby
        joinPanel.SetActive(false);
        mainPanel.SetActive(false);
    }

    private void OnClientDisconnected()
    {
        // Return to main menu on disconnect
        joinPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
}
