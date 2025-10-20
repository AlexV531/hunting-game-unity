using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerListManager : NetworkBehaviour
{
    public static PlayerListManager Instance;

    public NetworkList<PlayerData> playerList = new NetworkList<PlayerData>();

    public override void OnNetworkSpawn()
    {
        if (Instance == null)
            Instance = this;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnDestroy()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        base.OnDestroy();
    }

    // SERVER ONLY: Add new player to the NetworkList
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj == null) return;

        var player = playerObj.GetComponent<FirstPersonController>();
        if (player == null) return;

        playerList.Add(new PlayerData
        {
            ClientId = clientId,
            Name = player.PlayerName.Value,
            Kills = player.KillCount.Value
        });
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < playerList.Count; i++)
        {
            if (playerList[i].ClientId == clientId)
            {
                playerList.RemoveAt(i);
                break;
            }
        }
    }

    public NetworkList<PlayerData> GetPlayerList() => playerList;
}
