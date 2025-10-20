using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PlayerListUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentPanel;           // Parent UI container for entries
    public GameObject playerEntryPrefab;     // Prefab with TMP_Text child

    private Dictionary<ulong, GameObject> playerEntries = new();

    private void OnEnable()
    {
        StartCoroutine(InitializeWhenReady());
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private IEnumerator InitializeWhenReady()
    {
        // Wait until NetworkManager exists and the client is connected
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost) &&
            NetworkManager.Singleton.IsConnectedClient);

        // Wait until the local player object exists (network spawn complete)
        yield return new WaitUntil(() =>
            NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null);

        // Wait a short frame delay to ensure all player objects are spawned on the client
        yield return null;

        // Now it's safe to populate
        PopulateExistingPlayers();

        // Subscribe to connection changes
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    private void PopulateExistingPlayers()
    {
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            HandleClientConnected(kvp.Key);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        // Avoid duplicates
        if (playerEntries.ContainsKey(clientId)) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return;

        var playerObject = client.PlayerObject;
        if (playerObject == null)
            return;

        var player = playerObject.GetComponent<FirstPersonController>();
        if (player == null)
            return;

        // Instantiate entry
        var entry = Instantiate(playerEntryPrefab, contentPanel);
        var text = entry.GetComponentInChildren<TMP_Text>();

        void UpdateEntry()
        {
            text.text = $"{player.PlayerName.Value} - Kills: {player.KillCount.Value}";
        }

        UpdateEntry();

        // React to variable changes
        player.PlayerName.OnValueChanged += (_, __) => UpdateEntry();
        player.KillCount.OnValueChanged += (_, __) => UpdateEntry();

        playerEntries[clientId] = entry;

        Debug.Log($"[PlayerListUI] Added client {clientId} ({player.PlayerName.Value})");
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (playerEntries.TryGetValue(clientId, out var entry))
        {
            Destroy(entry);
            playerEntries.Remove(clientId);
            Debug.Log($"[PlayerListUI] Removed client {clientId}");
        }
    }
}
