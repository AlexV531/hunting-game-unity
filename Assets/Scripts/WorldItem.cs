using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class WorldItem : InteractableBase
{
    [SerializeField] private int interactableLayer = 11;
    private Rigidbody rb;

    // Networked representation of the item
    private NetworkVariable<ItemInstance> netItemInstance = new(
        new ItemInstance(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool isPickedUp = false;
    private GameObject visualInstance;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        netItemInstance.OnValueChanged += OnItemChanged;

        if (IsClient)
            OnItemChanged(default, netItemInstance.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        netItemInstance.OnValueChanged -= OnItemChanged;
    }

    private void OnItemChanged(ItemInstance oldItem, ItemInstance newItem)
    {
        if (visualInstance)
            Destroy(visualInstance);

        ItemDefinition def = ItemDatabase.Instance.GetItem(newItem.key);

        if (def.worldAppearancePrefab)
        {
            visualInstance = Instantiate(def.worldAppearancePrefab, transform);
            LayerUtils.SetLayerRecursively(visualInstance, interactableLayer);
            visualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    // Called only by the server to initialize
    public void Initialize(ItemInstance item, Vector3 position, Quaternion rotation, Vector3 force)
    {
        transform.position = position;
        transform.rotation = rotation;
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);

        netItemInstance.Value = item;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPickupServerRpc(ulong playerId)
    {
        if (isPickedUp)
            return;

        isPickedUp = true;
        Debug.Log("Item picked up");

        ItemDefinition def = ItemDatabase.Instance.GetItem(netItemInstance.Value.key);

        if (def.itemSize == ItemSize.Small) // If item is small, add to inventory
        {
            AddItemToClientRpc(playerId);
            NetworkObject.Despawn();
        }
        else if (def.itemSize == ItemSize.Large) // If item is large, shoulder carry
        {
            
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
            {
                var player = client.PlayerObject.GetComponent<FirstPersonController>();
                if (player.IsShoulderCarrying.Value)
                {
                    isPickedUp = false;
                    return;
                }
                player.PickUpWorldItem(this);
                SetInteractionEnabled(false);
            }
            isPickedUp = false;
        }
    }

    [ClientRpc]
    void AddItemToClientRpc(ulong playerId)
    {
        if (NetworkManager.Singleton.LocalClientId != playerId)
            return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
        {
            var player = client.PlayerObject.GetComponent<FirstPersonController>();
            player.GetInventory().AddItem(netItemInstance.Value);
        }
    }

    public ItemInstance GetItemData() => netItemInstance.Value;

    public override void Interact(FirstPersonController player)
    {
        // Tell server to give the item to this player
        RequestPickupServerRpc(player.OwnerClientId);
    }

    public override string GetPrompt(FirstPersonController player)
    {
        // Can show the item name or stack size
        return $"Pick up {netItemInstance.Value.stackSize}x {ItemDatabase.Instance.GetItem(netItemInstance.Value.key).itemName}";
    }

    [ClientRpc]
    public void EnableCollidersClientRpc()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = true;
        }
        rb.isKinematic = false;
    }

    [ClientRpc]
    public void DisableCollidersClientRpc()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        rb.isKinematic = true;
    }
}
