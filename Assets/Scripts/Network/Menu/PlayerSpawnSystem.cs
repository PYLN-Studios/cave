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

        // Find spawn points in scene by tag
        spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint")
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
        Debug.Log($"SpawnPlayer called for connection: {conn.connectionId}");

        // Persist by stable player id; fallback to connection id if unavailable.
        NetworkGamePlayerLobby gamePlayer = conn.identity != null
            ? conn.identity.GetComponent<NetworkGamePlayerLobby>()
            : null;
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
            // Hydrate vitals before handing ownership to the connection.
            vitals.SetPersistenceKey(playerId);
            if (manager != null && manager.TryGetSavedVitals(playerId, out PlayerVitalsSaveData saveData))
            {
                vitals.ApplySavedData(saveData);
            }
        }

        // If you use spawn, isLocalPlayer doesn't work
        // NetworkServer.Spawn(playerInstance, conn);

        // Use ReplacePlayer rather than AddPlayer because we have the GamePlayer already for this connection
        NetworkServer.ReplacePlayerForConnection(conn, playerInstance, ReplacePlayerOptions.KeepAuthority);
        Debug.Log($"Spawned player, hasAuthority: {playerInstance.GetComponent<NetworkIdentity>().isOwned}");
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
