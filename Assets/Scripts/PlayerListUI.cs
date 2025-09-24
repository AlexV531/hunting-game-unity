using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerListUI : MonoBehaviour
{
    public Transform contentPanel;   // Parent UI element for the list
    public GameObject playerEntryPrefab; // A prefab with a Text or TMP_Text component

    private Dictionary<ulong, GameObject> playerEntries = new Dictionary<ulong, GameObject>();

    private IEnumerator WaitForNetworkManager()
    {
        while (NetworkManager.Singleton == null)
            yield return null;

        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    private void OnEnable()
    {
        StartCoroutine(WaitForNetworkManager());
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObject == null) return;

        var player = playerObject.GetComponent<FirstPersonController>();
        if (player == null) return;

        // Create entry
        Debug.Log("client connected");
        var entry = Instantiate(playerEntryPrefab, contentPanel);
        var text = entry.GetComponentInChildren<TMPro.TMP_Text>();

        void UpdateEntry()
        {
            text.text = $"{player.PlayerName.Value} - Kills: {player.KillCount.Value}";
        }

        UpdateEntry();

        // React to changes
        player.PlayerName.OnValueChanged += (_, __) => UpdateEntry();
        player.KillCount.OnValueChanged += (_, __) => UpdateEntry();

        playerEntries[clientId] = entry;
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (playerEntries.TryGetValue(clientId, out var entry))
        {
            Destroy(entry);
            playerEntries.Remove(clientId);
        }
    }
}