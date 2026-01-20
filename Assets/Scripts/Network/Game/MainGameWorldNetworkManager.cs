using Mirror;
using System;
using UnityEngine;

public class MainGameWorldNetworkManager : Mirror.NetworkManager
{
    private MainGameWorldNetworkManager world;
    private MainGameWorldNetworkManager World
    {
        get
        {
            if (world != null) { return world; }
            return world = MainGameWorldNetworkManager.singleton as MainGameWorldNetworkManager;
        }
    }

    //    // TODO preload data
    //    [Command]
    //    public void SpawnProjectile(GameObject projectile)
    //    {
    //        NetworkServer.Spawn(projectile);
    //        Debug.Log("Spear spawned on network");
    //    }
}
