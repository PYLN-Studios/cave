using Mirror;
using UnityEngine;

public class NetworkGamePlayerLobby : NetworkBehaviour
{
    [SyncVar]
    public string DisplayName = "Loading...";

    [SyncVar]
    public string PlayerId = string.Empty;

    private NetworkManagerLobby room;
    private NetworkManagerLobby Room
    {
        get
        {
            if (room != null) { return room; }
            return room = NetworkManager.singleton as NetworkManagerLobby;
        }
    }

    public override void OnStartClient()
    {
        DontDestroyOnLoad(gameObject);
        Room.GamePlayers.Add(this);
    }

    public override void OnStopClient()
    {
        Room.GamePlayers.Remove(this);
    }

    [Server]
    public void SetDisplayName(string displayName)
    {
        DisplayName = displayName;
    }

    [Server]
    public void SetPlayerIdentity(string playerId)
    {
        PlayerId = playerId;
    }
}
