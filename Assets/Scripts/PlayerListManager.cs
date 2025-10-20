using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerListManager : NetworkBehaviour
{
    public static PlayerListManager Instance { get; private set; }

    public NetworkList<PlayerData> Players = new NetworkList<PlayerData>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        var player = playerObj.GetComponent<FirstPersonController>();

        Players.Add(new PlayerData
        {
            ClientId = clientId,
            Name = player.PlayerName.Value,
            Kills = player.KillCount.Value
        });

        // Subscribe to live updates
        player.PlayerName.OnValueChanged += (oldVal, newVal) => UpdatePlayer(clientId, newVal, player.KillCount.Value);
        player.KillCount.OnValueChanged += (oldVal, newVal) => UpdatePlayer(clientId, player.PlayerName.Value, newVal);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                Players.RemoveAt(i);
                break;
            }
        }
    }

    private void UpdatePlayer(ulong clientId, FixedString64Bytes name, int kills)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                Players[i] = new PlayerData { ClientId = clientId, Name = name, Kills = kills };
                break;
            }
        }
    }
}
