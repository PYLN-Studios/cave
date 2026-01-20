using Mirror;
using System;
using UnityEngine;

public class MainGameWorldNetworkManager : Mirror.NetworkManager
{
    // TODO preload data
    [Command]
    public void SpawnProjectile(GameObject projectile)
    {
        NetworkServer.Spawn(projectile);
        Debug.Log("Spear spawned on network");
    }
}
