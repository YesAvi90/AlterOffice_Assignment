using Mirror;
using UnityEngine;

public class Matchmaking : NetworkManager
{
    [Header("1v1 Settings")]
    public int maxPlayers = 2;

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (NetworkServer.connections.Count > maxPlayers)           // If game already has 2 players, reject
        {
            Debug.Log("[SERVER] Connection rejected: match already full " + NetworkServer.connections.Count);
            conn.Disconnect();
            return;
        }

        Debug.Log("[SERVER] Client connected");
        base.OnServerConnect(conn);
    }

    public override void OnStartServer()
    {
        Debug.Log("[SERVER] Matchmaking server started");
        base.OnStartServer();
    }

    public override void OnClientConnect()
    {
        Debug.Log("[CLIENT] Connected to host");
        base.OnClientConnect();
    }


    public override void OnClientDisconnect()
    {
        if (CanvasControl.Instance != null)
        {
            CanvasControl.Instance.ResetGameState();
            CanvasControl.Instance.ShowMatchmakingUIWithMessage("Disconnected");
        }

        base.OnClientDisconnect();

        Debug.Log("[MATCHMAKING] Client disconnected");

    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (NetworkServer.connections.Count == 0)
        {
            StopHost();
        }

        if (CanvasControl.Instance != null)
            CanvasControl.Instance.ShowMatchmakingUIWithMessage("Opponent left the match");

        base.OnServerDisconnect(conn);

        Debug.Log("[MATCHMAKING] Player disconnected from server");

    }


}
