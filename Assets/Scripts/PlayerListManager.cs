using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerListManager : NetworkBehaviour
{
    public static PlayerListManager Instance;
    public NetworkList<PlayerData> playerList;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        // Create list
        playerList = new NetworkList<PlayerData>();
    }

    public override void OnNetworkSpawn()
    {
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

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj == null) return;

        var player = playerObj.GetComponent<FirstPersonController>();
        if (player == null) return;

        playerList.Add(new PlayerData(clientId, player.PlayerName.Value, player.KillCount.Value));

        player.KillCount.OnValueChanged += (oldVal, newVal) =>
        {
            UpdatePlayerKills(clientId, newVal);
        };
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

    private void UpdatePlayerKills(ulong clientId, int newKills)
    {
        for (int i = 0; i < playerList.Count; i++)
        {
            if (playerList[i].ClientId == clientId)
            {
                var pd = playerList[i];
                pd.Kills = newKills;
                playerList[i] = pd;
                break;
            }
        }
    }

    public NetworkList<PlayerData> GetPlayerList() => playerList;
}
