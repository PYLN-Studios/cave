using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSpawnSystem : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab = null;
    [SerializeField] private Transform[] spawnPoints = null;

    private static List<Transform> availableSpawnPoints = new List<Transform>();

    private int nextSpawnIndex = 0;

    public override void OnStartServer()
    {
        NetworkManagerLobby.OnServerReadied += SpawnPlayer;

        // Populate spawn points
        availableSpawnPoints = spawnPoints.ToList();
    }

    private void OnDestroy()
    {
        NetworkManagerLobby.OnServerReadied -= SpawnPlayer;
    }

    [Server]
    public void SpawnPlayer(NetworkConnectionToClient conn)
    {
        // Get spawn position
        Transform spawnPoint = GetNextSpawnPoint();
        
        // Spawn the player
        GameObject playerInstance = Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        NetworkServer.Spawn(playerInstance, conn);
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
