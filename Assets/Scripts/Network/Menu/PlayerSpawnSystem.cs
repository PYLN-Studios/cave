using Mirror;
using System.Linq;
using UnityEngine;
using Player;

public class PlayerSpawnSystem : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab = null;
    [SerializeField] private Transform[] spawnPoints = null;

    private int nextSpawnIndex = 0;

    public override void OnStartServer()
    {
        NetworkManagerLobby.OnServerReadied += SpawnPlayer;

        // Find spawn points in scene by tag and keep deterministic ordering.
        spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint")
            .OrderBy(go => go.name)
            .Select(go => go.transform)
            .ToArray();
    }

    private void OnDestroy()
    {
        NetworkManagerLobby.OnServerReadied -= SpawnPlayer;
    }

    [Server]
    public void SpawnPlayer(NetworkConnectionToClient conn)
    {
        if (conn == null)
        {
            return;
        }

        if (conn.identity == null)
        {
            return;
        }

        // Spawn should only happen when connection currently owns the game lobby placeholder.
        NetworkGamePlayerLobby gamePlayer = conn.identity.GetComponent<NetworkGamePlayerLobby>();
        if (gamePlayer == null)
        {
            return;
        }

        // Persist by stable player id, fallback to connection id if unavailable.
        string playerId = gamePlayer != null && !string.IsNullOrWhiteSpace(gamePlayer.PlayerId)
            ? gamePlayer.PlayerId
            : $"conn:{conn.connectionId}";

        // Get spawn position
        Transform spawnPoint = GetNextSpawnPoint();
        
        // Spawn the player
        GameObject playerInstance = Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        PlayerVitals vitals = playerInstance.GetComponent<PlayerVitals>();
        NetworkManagerLobby manager = NetworkManager.singleton as NetworkManagerLobby;
        if (vitals != null)
        {
            // Load vitals before handing ownership to the connection
            vitals.SetPersistenceKey(playerId);
            if (manager != null && manager.TryGetSavedVitals(playerId, out PlayerVitalsSaveData saveData))
            {
                vitals.ApplySavedData(saveData);
            }
        }

        // Use ReplacePlayer rather than AddPlayer because we have the GamePlayer already for this connection.
        // Destroy old game-player placeholder so ownership/local-player state is fully transferred.
        bool replaced = NetworkServer.ReplacePlayerForConnection(conn, playerInstance, ReplacePlayerOptions.Destroy);
        if (!replaced)
        {
            Debug.LogError($"Failed to replace game player for conn {conn.connectionId}.");
            Destroy(playerInstance);
            return;
        }
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned to PlayerSpawnSystem!");
            return transform;
        }

        // Cycle through spawn points
        Transform spawnPoint = spawnPoints[nextSpawnIndex];
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;

        return spawnPoint;
    }
}
