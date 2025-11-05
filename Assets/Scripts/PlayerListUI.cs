using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class PlayerListUI : MonoBehaviour
{
    public Transform contentPanel;
    public GameObject playerEntryPrefab;

    private Dictionary<ulong, GameObject> entries = new();

    private void Start()
    {
        StartCoroutine(WaitForManager());
    }

    private IEnumerator WaitForManager()
    {
        // Wait until we’re connected
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            yield return null;

        // Wait until the PlayerListManager is spawned and synced
        while (PlayerListManager.Instance == null || PlayerListManager.Instance.GetPlayerList() == null)
            yield return null;

        var list = PlayerListManager.Instance.GetPlayerList();

        // Populate initial list
        foreach (var p in list)
            AddOrUpdate(p);

        // React to future updates
        list.OnListChanged += OnListChanged;
    }

    private void OnListChanged(NetworkListEvent<PlayerData> change)
    {
        RebuildAll();
    }

    private void AddOrUpdate(PlayerData data)
    {
        if (!entries.TryGetValue(data.ClientId, out var entry))
        {
            entry = Instantiate(playerEntryPrefab, contentPanel);
            entries[data.ClientId] = entry;
        }

        var text = entry.GetComponentInChildren<TMP_Text>();
        text.text = $"{data.Name} - Kills: {data.Kills}";
    }

    private void Remove(ulong clientId)
    {
        if (entries.TryGetValue(clientId, out var entry))
        {
            Destroy(entry);
            entries.Remove(clientId);
        }
    }

    private void HandleRemove(int index)
    {
        var list = PlayerListManager.Instance.GetPlayerList();
        if (index < 0 || index >= list.Count)
        {
            // If we removed the last item, index may now be out of range.
            // Safest way: rebuild list from scratch.
            RebuildAll();
            return;
        }

        var removedId = list[index].ClientId;
        Remove(removedId);
    }

    private void RebuildAll()
    {
        // Clear current entries
        foreach (var kvp in entries)
            Destroy(kvp.Value);
        entries.Clear();

        // Rebuild from authoritative list
        var list = PlayerListManager.Instance.GetPlayerList();
        foreach (var p in list)
            AddOrUpdate(p);
    }

    private void ClearAll()
    {
        foreach (var kvp in entries)
            Destroy(kvp.Value);
        entries.Clear();
    }
}
