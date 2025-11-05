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
    private bool visualInitialized = false;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!IsClient || visualInitialized)
            return;

        // Only initialize once the data is valid
        var item = netItemInstance.Value;
        if (!item.Equals(default))
        {
            InitializeVisual();
            visualInitialized = true;
        }
    }

    private void ApplyCustomVisualData(GameObject visual, ItemInstance itemInstance)
    {
        ItemDefinition def = ItemDatabase.Instance.GetItem(itemInstance.key);
        if (def.itemType == ItemType.AnimalPelt)
        {
            // Get the mesh renderer (adjust the path if needed)
            MeshRenderer meshRenderer = visual.GetComponentInChildren<MeshRenderer>();

            if (meshRenderer != null && meshRenderer.materials.Length > 0)
            {
                // Create a new material instance to avoid modifying the shared material
                Material mat = meshRenderer.materials[0];
                mat = new Material(mat); // Clone the material

                mat.color = itemInstance.customData.color;

                // Apply the modified material back
                Material[] materials = meshRenderer.materials;
                materials[0] = mat;
                meshRenderer.materials = materials;
            }
        }
    }

    public void Initialize(ItemInstance item, Vector3 position, Quaternion rotation, Vector3 force)
    {
        transform.position = position;
        transform.rotation = rotation;
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);

        netItemInstance.Value = item;
        
        // Initialize visual on server
        InitializeVisual(item);
        
        // Tell all clients to initialize their visuals
        InitializeVisualClientRpc(item);
    }

    [ClientRpc]
    private void InitializeVisualClientRpc(ItemInstance item)
    {
        if (IsServer) return;
        InitializeVisual(item);
    }

    private void InitializeVisual(ItemInstance item)
    {
        ItemDefinition def = ItemDatabase.Instance.GetItem(item.key);

        if (def != null && def.worldAppearancePrefab)
        {
            visualInstance = Instantiate(def.worldAppearancePrefab, transform);
            LayerUtils.SetLayerRecursively(visualInstance, interactableLayer);
            visualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            ApplyCustomVisualData(visualInstance, item);
        }
    }

    private void InitializeVisual()
    {
        // Avoid duplicates
        if (visualInstance != null)
            return;

        InitializeVisual(netItemInstance.Value);
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
                // player.PickUpWorldItem(this);
                player.PickUpWorldItemServerRpc(NetworkObject);
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

    public Rigidbody GetRigidbody() => rb;

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

    public void EnableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = true;
        }
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    public void DisableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    [ClientRpc]
    public void EnableCollidersClientRpc()
    {
        EnableColliders();
    }

    [ClientRpc]
    public void DisableCollidersClientRpc()
    {
        DisableColliders();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DespawnItemServerRpc()
    {
        // Actually despawn the NetworkObject
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}