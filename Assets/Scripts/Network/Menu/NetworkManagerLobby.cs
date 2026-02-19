using Mirror;
using System;
using System.Collections.Generic;
using System.IO;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManagerLobby : NetworkManager
{
    [Serializable]
    private class PlayerVitalsSaveFile
    {
        // JsonUtility cannot serialize top-level lists directly.
        public List<PlayerVitalsSaveData> entries = new List<PlayerVitalsSaveData>();
    }

    [SerializeField] private int minPlayers = 2;
    [Scene] [SerializeField] private string menuScene = string.Empty;

    [Header("Maps")]
    [SerializeField] private int numberOfRounds = 1;
    [SerializeField] private MapSet mapSet = null;

    [Header("Room")]
    [SerializeField] private NetworkRoomPlayerLobby roomPlayerPrefab = null;

    [Header("Game")]
    [SerializeField] private NetworkGamePlayerLobby gamePlayerPrefab = null;
    [SerializeField] private GameObject playerSpawnSystem = null;
    [SerializeField] private GameObject roundSystem = null;

    [SerializeField] private GameObject spearPrefab;
    private uint projectileGuid;

    [Header("Persistence")]
    [SerializeField] private float vitalsAutosaveInterval = 15f;

    private MapHandler mapHandler;
    private readonly Dictionary<string, PlayerVitalsSaveData> vitalsSaves = new Dictionary<string, PlayerVitalsSaveData>();
    private string vitalsSavePath;
    private float vitalsAutosaveTimer;
    private readonly HashSet<int> spawnedGameConnectionIds = new HashSet<int>();
    private int expectedGamePlayers;

    public static event Action OnClientConnected;
    public static event Action OnClientDisconnected;
    public static event Action<NetworkConnectionToClient> OnServerReadied;
    public static event Action OnServerStopped;

    public List<NetworkRoomPlayerLobby> RoomPlayers { get; } = new List<NetworkRoomPlayerLobby>();
    public List<NetworkGamePlayerLobby> GamePlayers { get; } = new List<NetworkGamePlayerLobby>();

    // Initialize persistence cache path and preload existing save data.
    public override void Awake()
    {
        base.Awake();
        vitalsSavePath = Path.Combine(Application.persistentDataPath, "player_vitals.json");
        LoadVitalsSavesFromDisk();
        vitalsAutosaveTimer = vitalsAutosaveInterval;
    }

    // Server-only autosave tick for currently connected players.
    public override void Update()
    {
        base.Update();

        if (!NetworkServer.active)
        {
            return;
        }

        vitalsAutosaveTimer -= Time.deltaTime;
        if (vitalsAutosaveTimer > 0f)
        {
            return;
        }

        SaveAllConnectedPlayerVitals();
        vitalsAutosaveTimer = vitalsAutosaveInterval;
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        OnClientConnected?.Invoke();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        OnClientDisconnected?.Invoke();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (numPlayers >= maxConnections)
        {
            conn.Disconnect();
            return;
        }

        if (SceneManager.GetActiveScene().path != menuScene)
        {
            conn.Disconnect();
            return;
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().path == menuScene)
        {
            bool isLeader = RoomPlayers.Count == 0;

            NetworkRoomPlayerLobby roomPlayerInstance = Instantiate(roomPlayerPrefab);
            roomPlayerInstance.IsLeader = isLeader;

            NetworkServer.AddPlayerForConnection(conn, roomPlayerInstance.gameObject);

            // Mark the connection as ready so it can send commands
            NetworkServer.SetClientReady(conn);
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        int connId = conn != null ? conn.connectionId : -1;
        string identityName = conn != null && conn.identity != null ? conn.identity.name : "null";
        Debug.Log($"OnServerDisconnect conn={connId} identity={identityName} expectedGamePlayers={expectedGamePlayers}");

        SavePlayerVitalsFromConnection(conn);

        if (conn.identity != null)
        {
            var player = conn.identity.GetComponent<NetworkRoomPlayerLobby>();

            if (player != null)
            {
                RoomPlayers.Remove(player);
                NotifyPlayersOfReadyState();
            }
        }

        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        SaveAllConnectedPlayerVitals();
        OnServerStopped?.Invoke();
        RoomPlayers.Clear();
        GamePlayers.Clear();
        spawnedGameConnectionIds.Clear();
        expectedGamePlayers = 0;
    }

    public void NotifyPlayersOfReadyState()
    {
        foreach (var player in RoomPlayers)
        {
            player.HandleReadyToStart(IsReadyToStart());
        }
    }

    private bool IsReadyToStart()
    {
        if (numPlayers < minPlayers) { return false; }

        foreach (var player in RoomPlayers) {
            if (!player.IsReady) { return false; }
        }

        return true;
    }

    public void StartGame()
    {
        if (SceneManager.GetActiveScene().path == menuScene)
        {
            if (!IsReadyToStart()) { return; }
            
            SetupProjectilePrefabs();
    
            mapHandler = new MapHandler(mapSet, numberOfRounds);
            ServerChangeScene(mapHandler.NextMap);
        }
    }

    public override void ServerChangeScene(string newSceneName)
    {
        // From menu to game
        if (SceneManager.GetActiveScene().path == menuScene)
        {
            spawnedGameConnectionIds.Clear();
            expectedGamePlayers = 0;
            Debug.Log($"ServerChangeScene {menuScene} -> {newSceneName}. Building game placeholders for {RoomPlayers.Count} room players.");

            for (int i = RoomPlayers.Count - 1; i >= 0; i--)
            {
                var roomPlayer = RoomPlayers[i];
                if (roomPlayer == null || roomPlayer.connectionToClient == null)
                {
                    continue;
                }

                var conn = roomPlayer.connectionToClient;
                var gamePlayerInstance = Instantiate(gamePlayerPrefab);
                gamePlayerInstance.SetDisplayName(roomPlayer.DisplayName);
                string playerId = string.IsNullOrWhiteSpace(roomPlayer.PlayerId)
                    ? $"conn:{conn.connectionId}"
                    : roomPlayer.PlayerId;
                gamePlayerInstance.SetPlayerIdentity(playerId);

                // Replace and destroy the previous room player in one step.
                bool replaced = NetworkServer.ReplacePlayerForConnection(
                    conn,
                    gamePlayerInstance.gameObject,
                    ReplacePlayerOptions.Destroy
                );

                if (!replaced)
                {
                    Debug.LogError($"Failed to replace room player for conn {conn.connectionId} during scene change.");
                    Destroy(gamePlayerInstance.gameObject);
                }
                else
                {
                    expectedGamePlayers++;
                    Debug.Log($"Prepared game placeholder for conn {conn.connectionId}. expectedGamePlayers={expectedGamePlayers}");
                }
            }
        }

        base.ServerChangeScene(newSceneName);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        Debug.Log($"OnServerSceneChanged scene={sceneName} expectedGamePlayers={expectedGamePlayers}");

        if (playerSpawnSystem != null)
        {
            GameObject playerSpawnSystemInstance = Instantiate(playerSpawnSystem);
            NetworkServer.Spawn(playerSpawnSystemInstance);
        }

        if (roundSystem != null)
        {
            GameObject roundSystemInstance = Instantiate(roundSystem);
            NetworkServer.Spawn(roundSystemInstance);
        }
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);
        if (conn == null)
        {
            return;
        }

        string identityType = "null";
        if (conn.identity != null)
        {
            if (conn.identity.GetComponent<NetworkGamePlayerLobby>() != null)
            {
                identityType = nameof(NetworkGamePlayerLobby);
            }
            else if (conn.identity.GetComponent<NetworkRoomPlayerLobby>() != null)
            {
                identityType = nameof(NetworkRoomPlayerLobby);
            }
            else
            {
                identityType = conn.identity.name;
            }
        }
        Debug.Log($"OnServerReady conn={conn.connectionId} scene={SceneManager.GetActiveScene().path} identity={identityType} isReady={conn.isReady}");

        if (conn.identity == null)
        {
            return;
        }

        // Only trigger game-world spawn when this connection still owns the game lobby placeholder.
        // If it already owns the real player, ignore duplicate Ready messages.
        if (conn.identity.GetComponent<NetworkGamePlayerLobby>() == null)
        {
            return;
        }

        TrySpawnReadyGamePlayers();
    }

    [Server]
    private void TrySpawnReadyGamePlayers()
    {
        // Only barrier-spawn in gameplay scenes.
        if (SceneManager.GetActiveScene().path == menuScene)
        {
            Debug.Log("Spawn barrier skipped: still in menu scene.");
            return;
        }

        if (expectedGamePlayers <= 0)
        {
            Debug.Log("Spawn barrier skipped: expectedGamePlayers <= 0.");
            return;
        }

        List<NetworkConnectionToClient> gameConnections = new List<NetworkConnectionToClient>();
        foreach (NetworkConnectionToClient candidate in NetworkServer.connections.Values)
        {
            if (candidate == null || candidate.identity == null)
            {
                continue;
            }

            if (candidate.identity.GetComponent<NetworkGamePlayerLobby>() == null)
            {
                continue;
            }

            gameConnections.Add(candidate);
        }

        // Wait until all expected game players exist and are ready.
        if (gameConnections.Count < expectedGamePlayers)
        {
            Debug.Log($"Spawn barrier waiting: found {gameConnections.Count}/{expectedGamePlayers} gameplay connections.");
            return;
        }

        foreach (NetworkConnectionToClient candidate in gameConnections)
        {
            if (!candidate.isReady)
            {
                Debug.Log($"Spawn barrier waiting: conn {candidate.connectionId} is not ready yet.");
                return;
            }
        }

        Debug.Log($"Spawn barrier open: spawning {gameConnections.Count} gameplay connections in deterministic order.");
        gameConnections.Sort((a, b) => a.connectionId.CompareTo(b.connectionId));
        foreach (NetworkConnectionToClient readyConn in gameConnections)
        {
            if (!spawnedGameConnectionIds.Add(readyConn.connectionId))
            {
                Debug.Log($"Spawn barrier skip: conn {readyConn.connectionId} already spawned.");
                continue;
            }

            Debug.Log($"Spawn barrier release conn {readyConn.connectionId}");
            OnServerReadied?.Invoke(readyConn);
        }
    }

    #region projectiles spawning
    private void SetupProjectilePrefabs()
    {
        // Get the existing assetId from the prefab's NetworkIdentity
        NetworkIdentity identity = spearPrefab.GetComponent<NetworkIdentity>();
        if (identity != null)
        {
            projectileGuid = identity.assetId;
            Debug.Log("Registering spear with existing asset ID: " + projectileGuid);
            NetworkClient.RegisterPrefab(spearPrefab, projectileGuid, SpawnProjectile, UnSpawnProjectile);
        }
        else
        {
            Debug.LogError("Spear prefab missing NetworkIdentity component!");
        }
    }

    // Used by NetworkClient.RegisterPrefab
    public GameObject SpawnProjectile(SpawnMessage msg)
    {
        Debug.Log("Spawning projectile with assetId: " + msg.assetId);
        var newProjectile = Instantiate(spearPrefab, msg.position, msg.rotation);
        return newProjectile;
    }

    // Used by NetworkClient.RegisterPrefab
    public void UnSpawnProjectile(GameObject spawned)
    {
        Debug.Log("Unspawning projectile" + spawned.name);
        Destroy(spawned);
    }
    #endregion

    [Server]
    public bool TryGetSavedVitals(string playerId, out PlayerVitalsSaveData data)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            data = null;
            return false;
        }

        return vitalsSaves.TryGetValue(playerId, out data);
    }

    [Server]
    public void SavePlayerVitals(PlayerVitals vitals)
    {
        if (vitals == null)
        {
            return;
        }

        PlayerVitalsSaveData data = vitals.CreateSaveData();
        if (string.IsNullOrWhiteSpace(data.playerId))
        {
            return;
        }

        vitalsSaves[data.playerId] = data;
    }

    [Server]
    private void SavePlayerVitalsFromConnection(NetworkConnectionToClient conn)
    {
        if (conn == null || conn.identity == null)
        {
            return;
        }

        PlayerVitals vitals = conn.identity.GetComponent<PlayerVitals>();
        if (vitals == null)
        {
            return;
        }

        SavePlayerVitals(vitals);
        WriteVitalsSavesToDisk();
    }

    [Server]
    private void SaveAllConnectedPlayerVitals()
    {
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null)
            {
                continue;
            }

            PlayerVitals vitals = conn.identity.GetComponent<PlayerVitals>();
            if (vitals == null)
            {
                continue;
            }

            SavePlayerVitals(vitals);
        }

        WriteVitalsSavesToDisk();
    }

    // Load all previously persisted player vitals into memory cache.
    private void LoadVitalsSavesFromDisk()
    {
        vitalsSaves.Clear();

        if (!File.Exists(vitalsSavePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(vitalsSavePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            PlayerVitalsSaveFile loaded = JsonUtility.FromJson<PlayerVitalsSaveFile>(json);
            if (loaded == null || loaded.entries == null)
            {
                return;
            }

            foreach (PlayerVitalsSaveData entry in loaded.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.playerId))
                {
                    continue;
                }

                vitalsSaves[entry.playerId] = entry;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load player vitals save data: {ex.Message}");
        }
    }

    // Flush in-memory vitals cache to disk as JSON.
    private void WriteVitalsSavesToDisk()
    {
        try
        {
            PlayerVitalsSaveFile saveFile = new PlayerVitalsSaveFile();
            foreach (PlayerVitalsSaveData entry in vitalsSaves.Values)
            {
                saveFile.entries.Add(entry);
            }

            string json = JsonUtility.ToJson(saveFile, true);
            File.WriteAllText(vitalsSavePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to write player vitals save data: {ex.Message}");
        }
    }

}
